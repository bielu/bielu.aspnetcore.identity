using System.DirectoryServices.Protocols;
using System.Net;
using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bielu.AspNetCore.Identity.Ldap.Internal;

/// <summary>
/// Default <see cref="ILdapService"/> implementation backed by
/// <see cref="System.DirectoryServices.Protocols"/>.
/// </summary>
internal sealed class LdapService : ILdapService
{
    private readonly IOptionsMonitor<LdapOptions> _options;
    private readonly ILogger<LdapService> _logger;

    public LdapService(IOptionsMonitor<LdapOptions> options, ILogger<LdapService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        var userEntry = await FindUserAsync(username, cancellationToken).ConfigureAwait(false);
        if (userEntry is null)
        {
            _logger.LogWarning("User '{Username}' not found in the LDAP directory.", username);
            return false;
        }

        try
        {
            using var connection = CreateConnection();
            // LdapConnection.Bind is synchronous — no async Bind API exists
            // in System.DirectoryServices.Protocols. This is unavoidable.
            connection.Bind(new NetworkCredential(userEntry.Dn, password));
            return true;
        }
        catch (LdapException ex) when (ex.ErrorCode == 49) // InvalidCredentials
        {
            _logger.LogWarning("Invalid credentials for user '{Username}'.", username);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while validating credentials for '{Username}'.", username);
            return false;
        }
    }

    /// <inheritdoc/>
    public Task<LdapEntry?> FindUserAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(username);

        var filter = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            (_options.CurrentValue).UserSearchFilter,
            EscapeLdapFilter(username));

        return SearchSingleAsync((_options.CurrentValue).UserSearchBase, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<LdapEntry?> FindUserByDnAsync(string dn, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dn);

        return SearchSingleAsync(dn, "(objectClass=*)", cancellationToken, SearchScope.Base);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<LdapEntry>> FindUsersAsync(string filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return SearchAsync((_options.CurrentValue).UserSearchBase, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<LdapEntry>> FindGroupsAsync(CancellationToken cancellationToken = default)
    {
        return SearchAsync((_options.CurrentValue).GroupSearchBase, (_options.CurrentValue).GroupListFilter, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<LdapEntry>> GetUserGroupsAsync(string userDn, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userDn);

        var filter = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            (_options.CurrentValue).GroupSearchFilter,
            EscapeLdapFilter(userDn));

        return SearchAsync((_options.CurrentValue).GroupSearchBase, filter, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private LdapDirectoryIdentifier CreateIdentifier() =>
        new((_options.CurrentValue).Host, (_options.CurrentValue).Port, fullyQualifiedDnsHostName: false, connectionless: false);

    private LdapConnection CreateConnection()
    {
        var identifier = CreateIdentifier();
        var connection = new LdapConnection(identifier)
        {
            Timeout = TimeSpan.FromSeconds((_options.CurrentValue).ConnectionTimeoutSeconds),
        };

        if ((_options.CurrentValue).UsesSsl)
        {
            connection.SessionOptions.SecureSocketLayer = true;
        }

        connection.SessionOptions.ProtocolVersion = 3;

        // Bind with service-account credentials, or anonymous if none configured.
        if (!string.IsNullOrEmpty((_options.CurrentValue).BindDn))
        {
            connection.Bind(new NetworkCredential((_options.CurrentValue).BindDn, (_options.CurrentValue).BindPassword));
        }
        else
        {
            connection.Bind();
        }

        return connection;
    }

    private async Task<LdapEntry?> SearchSingleAsync(
        string baseDn,
        string filter,
        CancellationToken cancellationToken,
        SearchScope scope = SearchScope.Subtree)
    {
        var results = await SearchAsync(baseDn, filter, cancellationToken, scope).ConfigureAwait(false);
        return results.Count > 0 ? results[0] : null;
    }

    private async Task<IReadOnlyList<LdapEntry>> SearchAsync(
        string baseDn,
        string filter,
        CancellationToken cancellationToken,
        SearchScope scope = SearchScope.Subtree)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // CreateConnection() performs a synchronous Bind(), but there is no
        // async Bind API on LdapConnection — this is unavoidable.
        using var connection = CreateConnection();

        var request = new SearchRequest(baseDn, filter, scope);

        // Use the APM-based BeginSendRequest/EndSendRequest instead of
        // Task.Run wrapping a synchronous SendRequest. This avoids occupying
        // a thread-pool thread for the duration of the network round-trip.
        //
        // Register a cancellation callback to dispose the connection so the
        // underlying LDAP request is aborted instead of continuing silently.
        using var ctr = cancellationToken.Register(() => connection.Dispose());

        SearchResponse response;
        try
        {
            response = (SearchResponse)await Task<DirectoryResponse>.Factory
                .FromAsync(
                    connection.BeginSendRequest(
                        request,
                        PartialResultProcessing.NoPartialResultSupport,
                        null,
                        null),
                    connection.EndSendRequest)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            // The connection was disposed by the cancellation callback.
            throw new OperationCanceledException(cancellationToken);
        }

        var entries = new List<LdapEntry>(response.Entries.Count);
        foreach (SearchResultEntry entry in response.Entries)
        {
            entries.Add(MapEntry(entry));
        }

        return entries.AsReadOnly();
    }

    private static LdapEntry MapEntry(SearchResultEntry entry)
    {
        var attributes = new Dictionary<string, IReadOnlyList<string>>(
            entry.Attributes.Count,
            StringComparer.OrdinalIgnoreCase);

        foreach (DirectoryAttribute attr in entry.Attributes.Values)
        {
            var values = new List<string>(attr.Count);
            for (var i = 0; i < attr.Count; i++)
            {
                if (attr[i] is string s)
                {
                    values.Add(s);
                }
                else if (attr[i] is byte[] bytes)
                {
                    values.Add(Convert.ToBase64String(bytes));
                }
            }

            attributes[attr.Name] = values.AsReadOnly();
        }

        return new LdapEntry { Dn = entry.DistinguishedName, Attributes = attributes };
    }

    /// <summary>
    /// Escapes special characters in an LDAP filter value per RFC 4515.
    /// </summary>
    private static string EscapeLdapFilter(string value) =>
        LdapFilterHelper.EscapeLdapFilter(value);
}

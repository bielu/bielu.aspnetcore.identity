using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Bielu.AspNetCore.Identity.Ldap.Stores;

/// <summary>
/// ASP.NET Core Identity <see cref="IUserStore{TUser}"/> backed by an LDAP directory.
/// </summary>
/// <remarks>
/// Supports user lookup and password validation via LDAP bind.
/// Write operations (create / update / delete) are not supported because LDAP directories
/// managed by Identity providers are typically read-only from the application's perspective.
/// </remarks>
public sealed class LdapUserStore : IUserStore<LdapUser>,
    IUserPasswordStore<LdapUser>,
    IUserEmailStore<LdapUser>
{
    private readonly ILdapService _ldapService;
    private readonly IOptionsMonitor<LdapOptions> _optionsMonitor;

    /// <summary>
    /// Initializes a new instance of <see cref="LdapUserStore"/>.
    /// </summary>
    public LdapUserStore(ILdapService ldapService, IOptionsMonitor<LdapOptions> options)
    {
        ArgumentNullException.ThrowIfNull(ldapService);
        ArgumentNullException.ThrowIfNull(options);

        _ldapService = ldapService;
        _optionsMonitor = options;
    }

    // -----------------------------------------------------------------------
    // IUserStore<LdapUser>
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IdentityResult> CreateAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        await Task.CompletedTask.ConfigureAwait(false);
        return IdentityResult.Failed(new IdentityError
        {
            Code = "LdapReadOnly",
            Description = "The LDAP user store does not support creating users."
        });
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> DeleteAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        await Task.CompletedTask.ConfigureAwait(false);
        return IdentityResult.Failed(new IdentityError
        {
            Code = "LdapReadOnly",
            Description = "The LDAP user store does not support deleting users."
        });
    }

    /// <inheritdoc/>
    public async Task<LdapUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        // userId is the username in the LDAP-backed store
        var entry = await _ldapService.FindUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return entry is null ? null : MapToUser(entry);
    }

    /// <inheritdoc/>
    public async Task<LdapUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedUserName);

        var entry = await _ldapService
            .FindUserAsync(normalizedUserName, cancellationToken)
            .ConfigureAwait(false);

        return entry is null ? null : MapToUser(entry);
    }

    /// <inheritdoc/>
    public Task<string?> GetNormalizedUserNameAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult<string?>(user.NormalizedUserName);
    }

    /// <inheritdoc/>
    public Task<string> GetUserIdAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.Id);
    }

    /// <inheritdoc/>
    public Task<string?> GetUserNameAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult<string?>(user.UserName);
    }

    /// <inheritdoc/>
    public Task SetNormalizedUserNameAsync(LdapUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetUserNameAsync(LdapUser user, string? userName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.UserName = userName;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        await Task.CompletedTask.ConfigureAwait(false);
        return IdentityResult.Failed(new IdentityError
        {
            Code = "LdapReadOnly",
            Description = "The LDAP user store does not support updating users."
        });
    }

    // -----------------------------------------------------------------------
    // IUserPasswordStore<LdapUser>
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public Task<string?> GetPasswordHashAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        // LDAP passwords are validated via bind; we return a sentinel so
        // ASP.NET Core Identity treats this user as having a password.
        return Task.FromResult<string?>(user.PasswordHash);
    }

    /// <inheritdoc/>
    public Task<bool> HasPasswordAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task SetPasswordHashAsync(LdapUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        // Not persisted — LDAP passwords are managed by the directory.
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    // -----------------------------------------------------------------------
    // IUserEmailStore<LdapUser>
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<LdapUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedEmail);

        var filter = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "(&(objectClass=person)({0}={1}))",
            _optionsMonitor.CurrentValue.EmailAttribute,
            LdapFilterHelper.EscapeLdapFilter(normalizedEmail.ToLowerInvariant()));

        var entries = await _ldapService.FindUsersAsync(filter, cancellationToken).ConfigureAwait(false);
        return entries.Count > 0 ? MapToUser(entries[0]) : null;
    }

    /// <inheritdoc/>
    public Task<string?> GetEmailAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult<string?>(user.Email);
    }

    /// <inheritdoc/>
    public Task<bool> GetEmailConfirmedAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.EmailConfirmed);
    }

    /// <inheritdoc/>
    public Task<string?> GetNormalizedEmailAsync(LdapUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult<string?>(user.NormalizedEmail);
    }

    /// <inheritdoc/>
    public Task SetEmailAsync(LdapUser user, string? email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.Email = email;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetEmailConfirmedAsync(LdapUser user, bool confirmed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetNormalizedEmailAsync(LdapUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // -----------------------------------------------------------------------
    // IDisposable
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose — the LdapService owns the connections.
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private LdapUser MapToUser(LdapEntry entry)
    {
        var username = entry.GetAttribute(_optionsMonitor.CurrentValue.UsernameAttribute) ?? entry.Dn;
        return new LdapUser
        {
            Id = username,
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = entry.GetAttribute(_optionsMonitor.CurrentValue.EmailAttribute),
            NormalizedEmail = entry.GetAttribute(_optionsMonitor.CurrentValue.EmailAttribute)?.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = entry.GetAttribute(_optionsMonitor.CurrentValue.DisplayNameAttribute),
            DistinguishedName = entry.Dn,
        };
    }
}

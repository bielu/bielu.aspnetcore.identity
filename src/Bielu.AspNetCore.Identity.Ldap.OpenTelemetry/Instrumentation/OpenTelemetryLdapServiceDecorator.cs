using System.Diagnostics;
using Bielu.AspNetCore.Identity.Ldap.Abstractions;

namespace Bielu.AspNetCore.Identity.Ldap.OpenTelemetry.Instrumentation;

/// <summary>
/// An <see cref="ILdapService"/> decorator that wraps every LDAP operation
/// with an OpenTelemetry <see cref="Activity"/> span and updates the
/// <see cref="LdapMetrics"/> counters and histograms.
/// </summary>
public sealed class OpenTelemetryLdapServiceDecorator : ILdapService
{
    private readonly ILdapService _inner;

    /// <summary>
    /// Initializes a new instance of <see cref="OpenTelemetryLdapServiceDecorator"/>.
    /// </summary>
    /// <param name="inner">The decorated <see cref="ILdapService"/> instance.</param>
    public OpenTelemetryLdapServiceDecorator(ILdapService inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var activity = LdapActivitySource.Source.StartActivity(LdapActivitySource.ValidateCredentials);
        activity?.SetTag(LdapActivitySource.AttributeUsername, username);

        var sw = Stopwatch.StartNew();
        LdapMetrics.AuthenticationAttempts.Add(1, new KeyValuePair<string, object?>("ldap.username", username));

        try
        {
            var result = await _inner.ValidateCredentialsAsync(username, password, cancellationToken)
                .ConfigureAwait(false);

            if (result)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                LdapMetrics.AuthenticationSuccesses.Add(1);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Invalid credentials");
                LdapMetrics.AuthenticationFailures.Add(1);
            }

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LdapMetrics.AuthenticationFailures.Add(1);
            LdapMetrics.OperationErrors.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.ValidateCredentials));
            throw;
        }
        finally
        {
            sw.Stop();
            LdapMetrics.AuthenticationDuration.Record(sw.Elapsed.TotalSeconds);
        }
    }

    /// <inheritdoc/>
    public async Task<LdapEntry?> FindUserAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        using var activity = LdapActivitySource.Source.StartActivity(LdapActivitySource.FindUser);
        activity?.SetTag(LdapActivitySource.AttributeUsername, username);

        var sw = Stopwatch.StartNew();
        LdapMetrics.SearchOperations.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindUser));

        try
        {
            var result = await _inner.FindUserAsync(username, cancellationToken).ConfigureAwait(false);
            activity?.SetTag(LdapActivitySource.AttributeResultCount, result is null ? 0 : 1);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LdapMetrics.OperationErrors.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindUser));
            throw;
        }
        finally
        {
            sw.Stop();
            LdapMetrics.SearchDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindUser));
        }
    }

    /// <inheritdoc/>
    public async Task<LdapEntry?> FindUserByDnAsync(
        string dn,
        CancellationToken cancellationToken = default)
    {
        using var activity = LdapActivitySource.Source.StartActivity(LdapActivitySource.FindUserByDn);
        activity?.SetTag(LdapActivitySource.AttributeUserDn, dn);

        var sw = Stopwatch.StartNew();
        LdapMetrics.SearchOperations.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindUserByDn));

        try
        {
            var result = await _inner.FindUserByDnAsync(dn, cancellationToken).ConfigureAwait(false);
            activity?.SetTag(LdapActivitySource.AttributeResultCount, result is null ? 0 : 1);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LdapMetrics.OperationErrors.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindUserByDn));
            throw;
        }
        finally
        {
            sw.Stop();
            LdapMetrics.SearchDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindUserByDn));
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LdapEntry>> FindUsersAsync(
        string filter,
        CancellationToken cancellationToken = default)
    {
        using var activity = LdapActivitySource.Source.StartActivity(LdapActivitySource.FindUsers);

        var sw = Stopwatch.StartNew();
        LdapMetrics.SearchOperations.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindUsers));

        try
        {
            var result = await _inner.FindUsersAsync(filter, cancellationToken).ConfigureAwait(false);
            activity?.SetTag(LdapActivitySource.AttributeResultCount, result.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LdapMetrics.OperationErrors.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindUsers));
            throw;
        }
        finally
        {
            sw.Stop();
            LdapMetrics.SearchDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindUsers));
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LdapEntry>> FindGroupsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = LdapActivitySource.Source.StartActivity(LdapActivitySource.FindGroups);

        var sw = Stopwatch.StartNew();
        LdapMetrics.SearchOperations.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindGroups));

        try
        {
            var result = await _inner.FindGroupsAsync(cancellationToken).ConfigureAwait(false);
            activity?.SetTag(LdapActivitySource.AttributeResultCount, result.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LdapMetrics.OperationErrors.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindGroups));
            throw;
        }
        finally
        {
            sw.Stop();
            LdapMetrics.SearchDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.FindGroups));
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LdapEntry>> GetUserGroupsAsync(
        string userDn,
        CancellationToken cancellationToken = default)
    {
        using var activity = LdapActivitySource.Source.StartActivity(LdapActivitySource.GetUserGroups);
        activity?.SetTag(LdapActivitySource.AttributeUserDn, userDn);

        var sw = Stopwatch.StartNew();
        LdapMetrics.SearchOperations.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.GetUserGroups));

        try
        {
            var result = await _inner.GetUserGroupsAsync(userDn, cancellationToken).ConfigureAwait(false);
            activity?.SetTag(LdapActivitySource.AttributeResultCount, result.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            LdapMetrics.OperationErrors.Add(1, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.GetUserGroups));
            throw;
        }
        finally
        {
            sw.Stop();
            LdapMetrics.SearchDuration.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>("ldap.operation", LdapActivitySource.GetUserGroups));
        }
    }
}

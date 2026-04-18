using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Services;

/// <summary>
/// Orchestrates sign-in across multiple identity sources:
/// <list type="number">
///   <item><b>LDAP</b> — validates credentials against the corporate LDAP directory.</item>
///   <item><b>Local</b> — falls back to the EF-backed password store only when LDAP is
///         unavailable (error/exception).  If LDAP explicitly rejects the credentials the
///         local store is <b>not</b> tried, preventing accidental bypass.</item>
/// </list>
/// Entra ID (OpenID Connect) is handled by the OIDC middleware and does not flow through
/// this service; see the <c>OnTokenValidated</c> callback in <c>Program.cs</c>.
/// </summary>
public sealed class HybridSignInService
{
    private readonly ILdapService _ldap;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IOptionsMonitor<LdapOptions> _ldapOptions;
    private readonly ILogger<HybridSignInService> _logger;

    public HybridSignInService(
        ILdapService ldap,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        IOptionsMonitor<LdapOptions> ldapOptions,
        ILogger<HybridSignInService> logger)
    {
        _ldap = ldap;
        _users = users;
        _signIn = signIn;
        _ldapOptions = ldapOptions;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to sign in the user, trying LDAP first.  Falls back to local password
    /// <b>only</b> if the LDAP server is unreachable or throws an error.  When LDAP
    /// explicitly rejects the credentials the call fails immediately.
    /// </summary>
    /// <param name="username">The username entered by the user.</param>
    /// <param name="password">The password entered by the user.</param>
    /// <returns>A <see cref="SignInResult"/> indicating the outcome.</returns>
    public async Task<SignInResult> PasswordSignInAsync(string username, string password)
    {
        // ── 1. Try LDAP ──────────────────────────────────────────────────
        var ldapResult = await TryLdapSignInAsync(username, password);

        if (ldapResult == LdapSignInOutcome.Success)
        {
            return SignInResult.Success;
        }

        // LDAP authoritatively rejected the credentials — do NOT fall back.
        if (ldapResult == LdapSignInOutcome.InvalidCredentials)
        {
            return SignInResult.Failed;
        }

        // ── 2. LDAP was unavailable — fall back to local (EF) password ──
        _logger.LogInformation("LDAP unavailable for {User}, falling back to local account", username);
        return await _signIn.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private enum LdapSignInOutcome { Success, InvalidCredentials, Unavailable }

    private async Task<LdapSignInOutcome> TryLdapSignInAsync(string username, string password)
    {
        try
        {
            var valid = await _ldap.ValidateCredentialsAsync(username, password);
            if (!valid)
            {
                return LdapSignInOutcome.InvalidCredentials;
            }

            _logger.LogInformation("LDAP credentials valid for {User}", username);

            // Find or provision the local EF user linked to the LDAP account.
            var user = await _users.FindByNameAsync(username);
            if (user is null)
            {
                var opts = _ldapOptions.CurrentValue;
                var ldapEntry = await _ldap.FindUserAsync(username);
                user = new ApplicationUser
                {
                    UserName = username,
                    Email = ldapEntry?.GetAttribute(opts.EmailAttribute),
                    DisplayName = ldapEntry?.GetAttribute(opts.DisplayNameAttribute),
                    AuthenticationSource = "LDAP",
                };

                var createResult = await _users.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogWarning("Failed to provision LDAP user {User}: {Errors}",
                        username, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    return LdapSignInOutcome.InvalidCredentials;
                }

                _logger.LogInformation("Provisioned new local account for LDAP user {User}", username);
            }

            await _signIn.SignInAsync(user, isPersistent: false);
            return LdapSignInOutcome.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LDAP authentication error for {User}", username);
            return LdapSignInOutcome.Unavailable;
        }
    }
}

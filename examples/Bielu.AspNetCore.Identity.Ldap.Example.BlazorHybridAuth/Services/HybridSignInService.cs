using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Data;
using Microsoft.AspNetCore.Identity;

namespace Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Services;

/// <summary>
/// Orchestrates sign-in across multiple identity sources:
/// <list type="number">
///   <item><b>LDAP</b> — validates credentials against the corporate LDAP directory.</item>
///   <item><b>Local</b> — falls back to the EF-backed password store.</item>
/// </list>
/// Entra ID (OpenID Connect) is handled by the OIDC middleware and does not flow through
/// this service; see the <c>OnTokenValidated</c> callback in <c>Program.cs</c>.
/// </summary>
public sealed class HybridSignInService
{
    private readonly ILdapService _ldap;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ILogger<HybridSignInService> _logger;

    public HybridSignInService(
        ILdapService ldap,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        ILogger<HybridSignInService> logger)
    {
        _ldap = ldap;
        _users = users;
        _signIn = signIn;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to sign in the user, trying LDAP first and falling back to local password.
    /// </summary>
    /// <param name="username">The username entered by the user.</param>
    /// <param name="password">The password entered by the user.</param>
    /// <returns>A <see cref="SignInResult"/> indicating the outcome.</returns>
    public async Task<SignInResult> PasswordSignInAsync(string username, string password)
    {
        // ── 1. Try LDAP ──────────────────────────────────────────────────
        if (await TryLdapSignInAsync(username, password))
        {
            return SignInResult.Success;
        }

        // ── 2. Fall back to local (EF) password ──────────────────────────
        _logger.LogInformation("LDAP auth failed for {User}, falling back to local account", username);
        return await _signIn.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private async Task<bool> TryLdapSignInAsync(string username, string password)
    {
        try
        {
            var valid = await _ldap.ValidateCredentialsAsync(username, password);
            if (!valid)
            {
                return false;
            }

            _logger.LogInformation("LDAP credentials valid for {User}", username);

            // Find or provision the local EF user linked to the LDAP account.
            var user = await _users.FindByNameAsync(username);
            if (user is null)
            {
                var ldapEntry = await _ldap.FindUserAsync(username);
                user = new ApplicationUser
                {
                    UserName = username,
                    Email = ldapEntry?.GetAttribute("mail"),
                    DisplayName = ldapEntry?.GetAttribute("cn"),
                    AuthenticationSource = "LDAP",
                };

                var createResult = await _users.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogWarning("Failed to provision LDAP user {User}: {Errors}",
                        username, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    return false;
                }

                _logger.LogInformation("Provisioned new local account for LDAP user {User}", username);
            }

            await _signIn.SignInAsync(user, isPersistent: false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LDAP authentication error for {User}", username);
            return false;
        }
    }
}

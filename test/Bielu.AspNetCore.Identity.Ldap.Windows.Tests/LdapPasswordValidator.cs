using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Bielu.AspNetCore.Identity.Ldap.Windows.Tests;

/// <summary>
/// An ASP.NET Core Identity <see cref="IPasswordValidator{TUser}"/> that validates
/// passwords by performing an LDAP bind, delegating to <see cref="ILdapService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Register this validator alongside <c>AddLdapStores</c> when using
/// <c>SignInManager.PasswordSignInAsync</c> with <see cref="LdapUser"/>. The default
/// Identity password-hasher flow (<c>IPasswordHasher&lt;LdapUser&gt;</c>) will not work
/// because LDAP users do not have locally stored password hashes. This validator bridges
/// that gap by delegating to <see cref="ILdapService.ValidateCredentialsAsync"/>.
/// </para>
/// <para>
/// This class lives in the test project as a reference implementation. Production apps
/// should register a similar validator via
/// <c>builder.Services.AddTransient&lt;IPasswordValidator&lt;LdapUser&gt;, LdapPasswordValidator&gt;()</c>.
/// </para>
/// </remarks>
internal sealed class LdapPasswordValidator : IPasswordValidator<LdapUser>
{
    private readonly ILdapService _ldapService;

    public LdapPasswordValidator(ILdapService ldapService)
    {
        ArgumentNullException.ThrowIfNull(ldapService);
        _ldapService = ldapService;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ValidateAsync(UserManager<LdapUser> manager, LdapUser user, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "EmptyPassword",
                Description = "Password cannot be empty.",
            });
        }

        var isValid = await _ldapService
            .ValidateCredentialsAsync(user.UserName!, password)
            .ConfigureAwait(false);

        return isValid
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidLdapCredentials",
                Description = "The supplied credentials were not accepted by the LDAP directory.",
            });
    }
}

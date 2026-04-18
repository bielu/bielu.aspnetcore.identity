using Microsoft.AspNetCore.Identity;

namespace Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Data;

/// <summary>
/// Application user that supports local accounts, LDAP-linked, and Entra ID-linked accounts.
/// EF Core stores the user; the <see cref="AuthenticationSource"/> property indicates where
/// the user originally authenticated from.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Indicates how this user was created / originally authenticated.
    /// Possible values: <c>"Local"</c>, <c>"LDAP"</c>, <c>"EntraId"</c>.
    /// </summary>
    public string AuthenticationSource { get; set; } = "Local";

    /// <summary>
    /// Display name populated from the external provider (LDAP cn, Entra ID name claim, etc.).
    /// </summary>
    public string? DisplayName { get; set; }
}

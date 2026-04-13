using Microsoft.AspNetCore.Identity;

namespace Bielu.AspNetCore.Identity.Ldap;

/// <summary>
/// Represents an ASP.NET Core Identity user backed by an LDAP directory entry.
/// </summary>
public class LdapUser : IdentityUser
{
    /// <summary>
    /// The user's full display name as stored in the LDAP directory
    /// (e.g. the <c>cn</c> attribute).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The Distinguished Name (DN) of the LDAP entry for this user.
    /// </summary>
    public string? DistinguishedName { get; set; }
}

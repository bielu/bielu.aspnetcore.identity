using Microsoft.AspNetCore.Identity;

namespace Bielu.AspNetCore.Identity.Ldap;

/// <summary>
/// Represents an ASP.NET Core Identity role backed by an LDAP group entry.
/// </summary>
public class LdapRole : IdentityRole
{
    /// <summary>
    /// The Distinguished Name (DN) of the LDAP group entry for this role.
    /// </summary>
    public string? DistinguishedName { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="LdapRole"/>.
    /// </summary>
    public LdapRole()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LdapRole"/> with the given role name.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    public LdapRole(string roleName) : base(roleName)
    {
    }
}

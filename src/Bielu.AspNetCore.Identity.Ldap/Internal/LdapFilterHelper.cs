namespace Bielu.AspNetCore.Identity.Ldap.Internal;

/// <summary>
/// Shared LDAP filter utilities.
/// </summary>
internal static class LdapFilterHelper
{
    /// <summary>
    /// Escapes special characters in an LDAP filter value per RFC 4515.
    /// </summary>
    internal static string EscapeLdapFilter(string value)
    {
        return value
            .Replace("\\", "\\5c", StringComparison.Ordinal)
            .Replace("*", "\\2a", StringComparison.Ordinal)
            .Replace("(", "\\28", StringComparison.Ordinal)
            .Replace(")", "\\29", StringComparison.Ordinal)
            .Replace("\0", "\\00", StringComparison.Ordinal);
    }
}

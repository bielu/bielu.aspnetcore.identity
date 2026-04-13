namespace Bielu.AspNetCore.Identity.Ldap.Abstractions;

/// <summary>
/// A lightweight, read-only representation of a single LDAP directory entry,
/// decoupled from any underlying LDAP library type.
/// </summary>
public sealed class LdapEntry
{
    /// <summary>
    /// The Distinguished Name of the LDAP entry.
    /// </summary>
    public string Dn { get; init; } = string.Empty;

    /// <summary>
    /// All attributes returned for this entry, keyed by attribute name.
    /// Each attribute may have multiple values.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Attributes { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the first value of the named attribute, or <c>null</c> if the attribute
    /// is not present or has no values.
    /// </summary>
    /// <param name="name">The attribute name (case-insensitive).</param>
    /// <returns>The first string value, or <c>null</c>.</returns>
    public string? GetAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Attributes.TryGetValue(name, out var values) && values.Count > 0
            ? values[0]
            : null;
    }
}

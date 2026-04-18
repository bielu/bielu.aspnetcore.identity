namespace Bielu.AspNetCore.Identity.Ldap.Abstractions;

/// <summary>
/// A lightweight, read-only representation of a single LDAP directory entry,
/// decoupled from any underlying LDAP library type.
/// </summary>
public sealed class LdapEntry
{
    private IReadOnlyDictionary<string, IReadOnlyList<string>> _attributes =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The Distinguished Name of the LDAP entry.
    /// </summary>
    public string Dn { get; init; } = string.Empty;

    /// <summary>
    /// All attributes returned for this entry, keyed by attribute name.
    /// Each attribute may have multiple values.
    /// The dictionary uses case-insensitive key comparison; if a caller assigns a
    /// case-sensitive dictionary, it is automatically wrapped to preserve
    /// case-insensitive lookups.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Attributes
    {
        get => _attributes;
        init
        {
            // Ensure case-insensitive lookup regardless of the supplied dictionary.
            if (value is Dictionary<string, IReadOnlyList<string>> dict &&
                dict.Comparer == StringComparer.OrdinalIgnoreCase)
            {
                _attributes = value;
            }
            else
            {
                _attributes = new Dictionary<string, IReadOnlyList<string>>(value, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

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

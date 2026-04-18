namespace Bielu.AspNetCore.Identity.Ldap.Abstractions;

/// <summary>
/// Provides high-level LDAP operations used by the ASP.NET Core Identity stores.
/// </summary>
public interface ILdapService
{
    /// <summary>
    /// Validates that the given <paramref name="username"/> and <paramref name="password"/>
    /// combination is accepted by the LDAP directory (performs a bind).
    /// </summary>
    /// <param name="username">The plain username (not a DN).</param>
    /// <param name="password">The plain-text password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the credentials are valid; <c>false</c> if the bind fails
    /// due to invalid credentials.
    /// </returns>
    Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the directory for a single user entry matching the given <paramref name="username"/>.
    /// </summary>
    /// <param name="username">The plain username to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching <see cref="LdapEntry"/>, or <c>null</c> if not found.</returns>
    Task<LdapEntry?> FindUserAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the directory for a user entry by their Distinguished Name.
    /// </summary>
    /// <param name="dn">The Distinguished Name to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching <see cref="LdapEntry"/>, or <c>null</c> if not found.</returns>
    Task<LdapEntry?> FindUserByDnAsync(string dn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the directory for user entries matching a raw LDAP filter.
    /// </summary>
    /// <param name="filter">An LDAP search filter string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All matching entries.</returns>
    Task<IReadOnlyList<LdapEntry>> FindUsersAsync(string filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the directory for group entries and returns them as role entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All group entries in the configured group search base.</returns>
    Task<IReadOnlyList<LdapEntry>> FindGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the directory for a single group entry by its Distinguished Name.
    /// </summary>
    /// <param name="dn">The Distinguished Name of the group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching <see cref="LdapEntry"/>, or <c>null</c> if not found.</returns>
    Task<LdapEntry?> FindGroupByDnAsync(string dn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the directory for a single group entry by its name attribute.
    /// </summary>
    /// <param name="groupName">The group name to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching <see cref="LdapEntry"/>, or <c>null</c> if not found.</returns>
    Task<LdapEntry?> FindGroupByNameAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all groups of which the given user DN is a member.
    /// </summary>
    /// <param name="userDn">The user's Distinguished Name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching group entries.</returns>
    Task<IReadOnlyList<LdapEntry>> GetUserGroupsAsync(string userDn, CancellationToken cancellationToken = default);
}

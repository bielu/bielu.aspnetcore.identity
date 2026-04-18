using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Bielu.AspNetCore.Identity.Ldap.Stores;

/// <summary>
/// ASP.NET Core Identity <see cref="IRoleStore{TRole}"/> backed by LDAP groups.
/// </summary>
/// <remarks>
/// Group entries in the LDAP directory are treated as roles.
/// Write operations are not supported.
/// </remarks>
public sealed class LdapRoleStore : IRoleStore<LdapRole>
{
    private readonly ILdapService _ldapService;
    private readonly IOptionsMonitor<LdapOptions> _optionsMonitor;

    /// <summary>
    /// Initializes a new instance of <see cref="LdapRoleStore"/>.
    /// </summary>
    public LdapRoleStore(ILdapService ldapService, IOptionsMonitor<LdapOptions> options)
    {
        ArgumentNullException.ThrowIfNull(ldapService);
        ArgumentNullException.ThrowIfNull(options);

        _ldapService = ldapService;
        _optionsMonitor = options;
    }

    // -----------------------------------------------------------------------
    // IRoleStore<LdapRole>
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IdentityResult> CreateAsync(LdapRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        await Task.CompletedTask.ConfigureAwait(false);
        return IdentityResult.Failed(new IdentityError
        {
            Code = "LdapReadOnly",
            Description = "The LDAP role store does not support creating roles."
        });
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> DeleteAsync(LdapRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        await Task.CompletedTask.ConfigureAwait(false);
        return IdentityResult.Failed(new IdentityError
        {
            Code = "LdapReadOnly",
            Description = "The LDAP role store does not support deleting roles."
        });
    }

    /// <inheritdoc/>
    public async Task<LdapRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roleId);

        // Role IDs are stored as Distinguished Names — do a targeted base-scope search.
        var entry = await _ldapService.FindGroupByDnAsync(roleId, cancellationToken).ConfigureAwait(false);

        return entry is null ? null : MapToRole(entry);
    }

    /// <inheritdoc/>
    public async Task<LdapRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedRoleName);

        // Use a targeted server-side search by the group name attribute.
        var entry = await _ldapService.FindGroupByNameAsync(normalizedRoleName, cancellationToken).ConfigureAwait(false);

        return entry is null ? null : MapToRole(entry);
    }

    /// <inheritdoc/>
    public Task<string?> GetNormalizedRoleNameAsync(LdapRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult<string?>(role.NormalizedName);
    }

    /// <inheritdoc/>
    public Task<string> GetRoleIdAsync(LdapRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult(role.Id);
    }

    /// <inheritdoc/>
    public Task<string?> GetRoleNameAsync(LdapRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        return Task.FromResult<string?>(role.Name);
    }

    /// <inheritdoc/>
    public Task SetNormalizedRoleNameAsync(LdapRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetRoleNameAsync(LdapRole role, string? roleName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        role.Name = roleName;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateAsync(LdapRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        await Task.CompletedTask.ConfigureAwait(false);
        return IdentityResult.Failed(new IdentityError
        {
            Code = "LdapReadOnly",
            Description = "The LDAP role store does not support updating roles."
        });
    }

    // -----------------------------------------------------------------------
    // IDisposable
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose.
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private LdapRole MapToRole(LdapEntry entry)
    {
        var name = entry.GetAttribute(_optionsMonitor.CurrentValue.GroupNameAttribute) ?? entry.Dn;
        return new LdapRole(name)
        {
            Id = entry.Dn,
            NormalizedName = name.ToUpperInvariant(),
            DistinguishedName = entry.Dn,
        };
    }
}

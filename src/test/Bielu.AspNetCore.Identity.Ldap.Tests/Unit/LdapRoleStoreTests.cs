using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Stores;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Tests.Unit;

public class LdapRoleStoreTests
{
    private static LdapOptions DefaultOptions() => new()
    {
        GroupNameAttribute = "cn",
    };

    private static IOptionsMonitor<LdapOptions> CreateOptionsMonitor(LdapOptions options)
    {
        var monitor = Substitute.For<IOptionsMonitor<LdapOptions>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }

    private static LdapEntry GroupEntry(string cn, string? dn = null) =>
        new()
        {
            Dn = dn ?? $"cn={cn},ou=groups,dc=example,dc=com",
            Attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["cn"] = new[] { cn },
            },
        };

    // -----------------------------------------------------------------------
    // FindByNameAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindByNameAsync_ReturnsRole_WhenFound()
    {
        var service = Substitute.For<ILdapService>();
        service.FindGroupByNameAsync("ADMINS", Arg.Any<CancellationToken>())
               .Returns(GroupEntry("admins"));

        var store = new LdapRoleStore(service, CreateOptionsMonitor(DefaultOptions()));

        var role = await store.FindByNameAsync("ADMINS", CancellationToken.None);

        role.ShouldNotBeNull();
        role.Name.ShouldBe("admins");
        role.NormalizedName.ShouldBe("ADMINS");
        role.DistinguishedName.ShouldBe("cn=admins,ou=groups,dc=example,dc=com");
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsNull_WhenNotFound()
    {
        var service = Substitute.For<ILdapService>();
        service.FindGroupByNameAsync("NOBODY", Arg.Any<CancellationToken>())
               .Returns((LdapEntry?)null);

        var store = new LdapRoleStore(service, CreateOptionsMonitor(DefaultOptions()));

        var role = await store.FindByNameAsync("NOBODY", CancellationToken.None);

        role.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // FindByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindByIdAsync_ReturnsRole_WhenFound()
    {
        var service = Substitute.For<ILdapService>();
        service.FindGroupByDnAsync("cn=developers,ou=groups,dc=example,dc=com", Arg.Any<CancellationToken>())
               .Returns(GroupEntry("developers"));

        var store = new LdapRoleStore(service, CreateOptionsMonitor(DefaultOptions()));

        // Role ID is the DN
        var role = await store.FindByIdAsync("cn=developers,ou=groups,dc=example,dc=com", CancellationToken.None);

        role.ShouldNotBeNull();
        role.Name.ShouldBe("developers");
        role.Id.ShouldBe("cn=developers,ou=groups,dc=example,dc=com");
    }

    // -----------------------------------------------------------------------
    // Write operations return failure
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ReturnsFailedResult()
    {
        var service = Substitute.For<ILdapService>();
        var store = new LdapRoleStore(service, CreateOptionsMonitor(DefaultOptions()));

        var result = await store.CreateAsync(new LdapRole("test"), CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == "LdapReadOnly");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailedResult()
    {
        var service = Substitute.For<ILdapService>();
        var store = new LdapRoleStore(service, CreateOptionsMonitor(DefaultOptions()));

        var result = await store.DeleteAsync(new LdapRole("test"), CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == "LdapReadOnly");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailedResult()
    {
        var service = Substitute.For<ILdapService>();
        var store = new LdapRoleStore(service, CreateOptionsMonitor(DefaultOptions()));

        var result = await store.UpdateAsync(new LdapRole("test"), CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == "LdapReadOnly");
    }

    // -----------------------------------------------------------------------
    // Null guard tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindByNameAsync_ThrowsOnNullName()
    {
        var service = Substitute.For<ILdapService>();
        var store = new LdapRoleStore(service, CreateOptionsMonitor(DefaultOptions()));

        await Should.ThrowAsync<ArgumentNullException>(() =>
            store.FindByNameAsync(null!, CancellationToken.None));
    }
}

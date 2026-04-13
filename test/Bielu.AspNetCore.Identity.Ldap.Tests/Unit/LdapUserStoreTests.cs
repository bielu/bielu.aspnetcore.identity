using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Stores;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Tests.Unit;

public class LdapUserStoreTests
{
    private static LdapOptions DefaultOptions() => new()
    {
        UsernameAttribute = "uid",
        EmailAttribute = "mail",
        DisplayNameAttribute = "cn",
    };

    private static IOptionsMonitor<LdapOptions> CreateOptionsMonitor(LdapOptions options)
    {
        var monitor = Substitute.For<IOptionsMonitor<LdapOptions>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }

    private static LdapEntry UserEntry(string uid, string? mail = null, string? cn = null) =>
        new()
        {
            Dn = $"uid={uid},ou=users,dc=example,dc=com",
            Attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["uid"] = new[] { uid },
                ["mail"] = new[] { mail ?? $"{uid}@example.com" },
                ["cn"] = new[] { cn ?? uid },
            },
        };

    // -----------------------------------------------------------------------
    // FindByNameAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindByNameAsync_ReturnsUser_WhenFound()
    {
        var service = Substitute.For<ILdapService>();
        service.FindUserAsync("jdoe", Arg.Any<CancellationToken>())
               .Returns(UserEntry("jdoe", "jdoe@example.com", "John Doe"));

        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        var user = await store.FindByNameAsync("JDOE", CancellationToken.None);

        user.ShouldNotBeNull();
        user.UserName.ShouldBe("jdoe");
        user.Email.ShouldBe("jdoe@example.com");
        user.DisplayName.ShouldBe("John Doe");
        user.DistinguishedName.ShouldBe("uid=jdoe,ou=users,dc=example,dc=com");
    }

    [Fact]
    public async Task FindByNameAsync_ReturnsNull_WhenNotFound()
    {
        var service = Substitute.For<ILdapService>();
        service.FindUserAsync("ghost", Arg.Any<CancellationToken>())
               .Returns((LdapEntry?)null);

        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        var user = await store.FindByNameAsync("GHOST", CancellationToken.None);

        user.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // FindByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindByIdAsync_ReturnsUser_WhenFound()
    {
        var service = Substitute.For<ILdapService>();
        service.FindUserAsync("alice", Arg.Any<CancellationToken>())
               .Returns(UserEntry("alice"));

        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        var user = await store.FindByIdAsync("alice", CancellationToken.None);

        user.ShouldNotBeNull();
        user.Id.ShouldBe("alice");
    }

    // -----------------------------------------------------------------------
    // FindByEmailAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindByEmailAsync_ReturnsUser_WhenFound()
    {
        var service = Substitute.For<ILdapService>();
        service.FindUsersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new List<LdapEntry> { UserEntry("bob", "bob@example.com") }.AsReadOnly());

        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        var user = await store.FindByEmailAsync("BOB@EXAMPLE.COM", CancellationToken.None);

        user.ShouldNotBeNull();
        user.UserName.ShouldBe("bob");
    }

    [Fact]
    public async Task FindByEmailAsync_ReturnsNull_WhenNotFound()
    {
        var service = Substitute.For<ILdapService>();
        service.FindUsersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new List<LdapEntry>().AsReadOnly());

        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        var user = await store.FindByEmailAsync("nobody@example.com", CancellationToken.None);

        user.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // Write operations return failure
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ReturnsFailedResult()
    {
        var service = Substitute.For<ILdapService>();
        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        var result = await store.CreateAsync(new LdapUser { UserName = "test" }, CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == "LdapReadOnly");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailedResult()
    {
        var service = Substitute.For<ILdapService>();
        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        var result = await store.DeleteAsync(new LdapUser { UserName = "test" }, CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == "LdapReadOnly");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailedResult()
    {
        var service = Substitute.For<ILdapService>();
        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        var result = await store.UpdateAsync(new LdapUser { UserName = "test" }, CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Code == "LdapReadOnly");
    }

    // -----------------------------------------------------------------------
    // Password store
    // -----------------------------------------------------------------------

    [Fact]
    public async Task HasPasswordAsync_AlwaysReturnsTrue()
    {
        var service = Substitute.For<ILdapService>();
        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        var result = await store.HasPasswordAsync(new LdapUser(), CancellationToken.None);

        result.ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // Null guard tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindByNameAsync_ThrowsOnNullName()
    {
        var service = Substitute.For<ILdapService>();
        var store = new LdapUserStore(service, CreateOptionsMonitor(DefaultOptions()));

        await Should.ThrowAsync<ArgumentNullException>(() =>
            store.FindByNameAsync(null!, CancellationToken.None));
    }
}

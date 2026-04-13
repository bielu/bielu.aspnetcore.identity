using System.Diagnostics;
using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.OpenTelemetry;
using Bielu.AspNetCore.Identity.Ldap.OpenTelemetry.Instrumentation;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Tests.Unit;

public class OpenTelemetryDecoratorTests
{
    // -----------------------------------------------------------------------
    // ValidateCredentialsAsync delegation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ValidateCredentials_DelegatesToInner_ReturnsTrue()
    {
        var inner = Substitute.For<ILdapService>();
        inner.ValidateCredentialsAsync("alice", "pass", Arg.Any<CancellationToken>()).Returns(true);

        var decorator = new OpenTelemetryLdapServiceDecorator(inner);
        var result = await decorator.ValidateCredentialsAsync("alice", "pass");

        result.ShouldBeTrue();
        await inner.Received(1).ValidateCredentialsAsync("alice", "pass", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateCredentials_DelegatesToInner_ReturnsFalse()
    {
        var inner = Substitute.For<ILdapService>();
        inner.ValidateCredentialsAsync("alice", "wrong", Arg.Any<CancellationToken>()).Returns(false);

        var decorator = new OpenTelemetryLdapServiceDecorator(inner);
        var result = await decorator.ValidateCredentialsAsync("alice", "wrong");

        result.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // FindUserAsync delegation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindUserAsync_DelegatesToInner_ReturnsEntry()
    {
        var expectedEntry = new LdapEntry { Dn = "uid=bob,dc=example,dc=com" };
        var inner = Substitute.For<ILdapService>();
        inner.FindUserAsync("bob", Arg.Any<CancellationToken>()).Returns(expectedEntry);

        var decorator = new OpenTelemetryLdapServiceDecorator(inner);
        var result = await decorator.FindUserAsync("bob");

        result.ShouldBe(expectedEntry);
        await inner.Received(1).FindUserAsync("bob", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindUserAsync_DelegatesToInner_ReturnsNull()
    {
        var inner = Substitute.For<ILdapService>();
        inner.FindUserAsync("ghost", Arg.Any<CancellationToken>()).Returns((LdapEntry?)null);

        var decorator = new OpenTelemetryLdapServiceDecorator(inner);
        var result = await decorator.FindUserAsync("ghost");

        result.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // FindUserByDnAsync delegation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindUserByDnAsync_DelegatesToInner()
    {
        var dn = "uid=carol,dc=example,dc=com";
        var expectedEntry = new LdapEntry { Dn = dn };
        var inner = Substitute.For<ILdapService>();
        inner.FindUserByDnAsync(dn, Arg.Any<CancellationToken>()).Returns(expectedEntry);

        var decorator = new OpenTelemetryLdapServiceDecorator(inner);
        var result = await decorator.FindUserByDnAsync(dn);

        result.ShouldBe(expectedEntry);
        await inner.Received(1).FindUserByDnAsync(dn, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // FindUsersAsync delegation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindUsersAsync_DelegatesToInner()
    {
        var entries = new List<LdapEntry>
        {
            new() { Dn = "uid=a,dc=example,dc=com" },
            new() { Dn = "uid=b,dc=example,dc=com" },
        }.AsReadOnly();

        var inner = Substitute.For<ILdapService>();
        inner.FindUsersAsync("(objectClass=person)", Arg.Any<CancellationToken>()).Returns(entries);

        var decorator = new OpenTelemetryLdapServiceDecorator(inner);
        var result = await decorator.FindUsersAsync("(objectClass=person)");

        result.Count.ShouldBe(2);
        await inner.Received(1).FindUsersAsync("(objectClass=person)", Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // FindGroupsAsync delegation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FindGroupsAsync_DelegatesToInner()
    {
        var groups = new List<LdapEntry>
        {
            new() { Dn = "cn=admins,ou=groups,dc=example,dc=com" },
        }.AsReadOnly();

        var inner = Substitute.For<ILdapService>();
        inner.FindGroupsAsync(Arg.Any<CancellationToken>()).Returns(groups);

        var decorator = new OpenTelemetryLdapServiceDecorator(inner);
        var result = await decorator.FindGroupsAsync();

        result.Count.ShouldBe(1);
        await inner.Received(1).FindGroupsAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetUserGroupsAsync delegation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUserGroupsAsync_DelegatesToInner()
    {
        var userDn = "uid=dave,dc=example,dc=com";
        var groups = new List<LdapEntry>
        {
            new() { Dn = "cn=devs,ou=groups,dc=example,dc=com" },
        }.AsReadOnly();

        var inner = Substitute.For<ILdapService>();
        inner.GetUserGroupsAsync(userDn, Arg.Any<CancellationToken>()).Returns(groups);

        var decorator = new OpenTelemetryLdapServiceDecorator(inner);
        var result = await decorator.GetUserGroupsAsync(userDn);

        result.Count.ShouldBe(1);
        await inner.Received(1).GetUserGroupsAsync(userDn, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Exception propagation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ValidateCredentials_PropagatesException()
    {
        var inner = Substitute.For<ILdapService>();
        inner.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(Task.FromException<bool>(new InvalidOperationException("LDAP server unreachable")));

        var decorator = new OpenTelemetryLdapServiceDecorator(inner);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            decorator.ValidateCredentialsAsync("user", "pass"));
    }

    // -----------------------------------------------------------------------
    // ActivitySource
    // -----------------------------------------------------------------------

    [Fact]
    public void LdapActivitySource_Name_MatchesAssemblyName()
    {
        LdapActivitySource.Name.ShouldBe("Bielu.AspNetCore.Identity.Ldap.OpenTelemetry");
    }

    [Fact]
    public void LdapActivitySource_Source_IsNotNull()
    {
        LdapActivitySource.Source.ShouldNotBeNull();
        LdapActivitySource.Source.Name.ShouldBe(LdapActivitySource.Name);
    }

    // -----------------------------------------------------------------------
    // Metrics
    // -----------------------------------------------------------------------

    [Fact]
    public void LdapMetrics_Name_MatchesAssemblyName()
    {
        LdapMetrics.Name.ShouldBe("Bielu.AspNetCore.Identity.Ldap.OpenTelemetry");
    }

    [Fact]
    public void LdapMetrics_Meter_IsNotNull()
    {
        LdapMetrics.Meter.ShouldNotBeNull();
        LdapMetrics.Meter.Name.ShouldBe(LdapMetrics.Name);
    }

    // -----------------------------------------------------------------------
    // Constructor null guard
    // -----------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsOnNullInner()
    {
        Should.Throw<ArgumentNullException>(() =>
            new OpenTelemetryLdapServiceDecorator(null!));
    }
}

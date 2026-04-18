using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Bielu.AspNetCore.Identity.Ldap.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Windows.Tests;

/// <summary>
/// Integration tests that exercise the ASP.NET Core Identity stores
/// (<see cref="LdapUserStore"/> and <see cref="LdapRoleStore"/>) end-to-end
/// against the live Windows AD / LDAP server.
/// </summary>
[Collection(LdapIntegrationCollection.Name)]
public class LdapIdentityStoreIntegrationTests
{
    private readonly LdapIntegrationFixture _fixture;

    public LdapIdentityStoreIntegrationTests(LdapIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // -----------------------------------------------------------------------
    // User store
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UserStore_FindByNameAsync_ReturnsUser_ForExistingUser()
    {
        _fixture.SkipIfUnavailable();

        using var provider = BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<LdapUser>>();

        var user = await userManager.FindByNameAsync(_fixture.Settings.Integration.TestUsername);

        user.ShouldNotBeNull();
        user.UserName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task UserStore_FindByEmailAsync_ReturnsUser_WhenEmailConfigured()
    {
        _fixture.SkipIfUnavailable();

        var expectedEmail = _fixture.Settings.Integration.TestUserEmail;
        if (string.IsNullOrEmpty(expectedEmail))
        {
            return;
        }

        using var provider = BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<LdapUser>>();

        var user = await userManager.FindByEmailAsync(expectedEmail);

        user.ShouldNotBeNull();
    }

    [Fact]
    public async Task UserStore_CheckPasswordAsync_ReturnsTrue_ForValidCredentials()
    {
        _fixture.SkipIfUnavailable();

        using var provider = BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<LdapUser>>();
        var signInManager = provider.GetRequiredService<SignInManager<LdapUser>>();

        var user = await userManager.FindByNameAsync(_fixture.Settings.Integration.TestUsername);
        user.ShouldNotBeNull();

        var result = await signInManager.CheckPasswordSignInAsync(
            user,
            _fixture.Settings.Integration.TestPassword,
            lockoutOnFailure: false);

        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task UserStore_CheckPasswordAsync_ReturnsFalse_ForWrongPassword()
    {
        _fixture.SkipIfUnavailable();

        using var provider = BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<LdapUser>>();
        var signInManager = provider.GetRequiredService<SignInManager<LdapUser>>();

        var user = await userManager.FindByNameAsync(_fixture.Settings.Integration.TestUsername);
        user.ShouldNotBeNull();

        var result = await signInManager.CheckPasswordSignInAsync(
            user,
            "definitely-wrong-password-xyz-1234",
            lockoutOnFailure: false);

        result.Succeeded.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // Role store
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RoleStore_FindByNameAsync_ReturnsRole_WhenGroupConfigured()
    {
        _fixture.SkipIfUnavailable();

        var groupName = _fixture.Settings.Integration.TestGroupName;
        if (string.IsNullOrEmpty(groupName))
        {
            return;
        }

        using var provider = BuildServiceProvider();
        var roleManager = provider.GetRequiredService<RoleManager<LdapRole>>();

        var role = await roleManager.FindByNameAsync(groupName);

        role.ShouldNotBeNull();
        role.Name.ShouldBe(groupName, StringCompareShould.IgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register an IHttpContextAccessor stub needed by SignInManager.
        services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor,
            Microsoft.AspNetCore.Http.HttpContextAccessor>();

        // Use the IConfiguration overload with the default section name constant —
        // this is the idiomatic path a real ASP.NET Core app would follow.
        services.AddLdapIdentity(_fixture.Configuration, LdapOptions.SectionName);

        // Wire up the LDAP password validator so that SignInManager calls LDAP bind.
        services.AddScoped<IUserPasswordStore<LdapUser>>(sp =>
            (IUserPasswordStore<LdapUser>)sp.GetRequiredService<IUserStore<LdapUser>>());

        services.AddScoped<IPasswordValidator<LdapUser>, LdapPasswordValidator>();

        return services.BuildServiceProvider();
    }
}

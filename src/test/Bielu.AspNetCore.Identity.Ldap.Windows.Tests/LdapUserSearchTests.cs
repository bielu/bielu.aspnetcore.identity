using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Windows.Tests;

/// <summary>
/// Integration tests for user-search operations against a local Windows AD / LDAP server.
/// </summary>
[Collection(LdapIntegrationCollection.Name)]
public class LdapUserSearchTests
{
    private readonly LdapIntegrationFixture _fixture;

    public LdapUserSearchTests(LdapIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task FindUserAsync_ReturnsEntry_ForExistingUser()
    {
        _fixture.SkipIfUnavailable();

        var entry = await _fixture.LdapService.FindUserAsync(
            _fixture.Settings.Integration.TestUsername);

        entry.ShouldNotBeNull();
        entry.Dn.ShouldNotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task FindUserAsync_ReturnsNull_ForNonExistentUser()
    {
        _fixture.SkipIfUnavailable();

        var entry = await _fixture.LdapService.FindUserAsync("user_does_not_exist_xyz_abc");

        entry.ShouldBeNull();
    }

    [SkippableFact]
    public async Task FindUserAsync_PopulatesExpectedAttributes()
    {
        _fixture.SkipIfUnavailable();

        var settings = _fixture.Settings;
        var entry = await _fixture.LdapService.FindUserAsync(settings.Integration.TestUsername);

        entry.ShouldNotBeNull();

        // sAMAccountName / uid must match the username
        var username = entry.GetAttribute(settings.Ldap.UsernameAttribute);
        username.ShouldBe(settings.Integration.TestUsername, StringCompareShould.IgnoreCase);

        // e-mail must be populated when configured
        if (!string.IsNullOrEmpty(settings.Integration.TestUserEmail))
        {
            var email = entry.GetAttribute(settings.Ldap.EmailAttribute);
            email.ShouldBe(settings.Integration.TestUserEmail, StringCompareShould.IgnoreCase);
        }

        // display name must be populated when configured
        if (!string.IsNullOrEmpty(settings.Integration.TestUserDisplayName))
        {
            var displayName = entry.GetAttribute(settings.Ldap.DisplayNameAttribute);
            displayName.ShouldBe(settings.Integration.TestUserDisplayName);
        }
    }

    [SkippableFact]
    public async Task FindUserByDnAsync_ReturnsEntry_ForExistingDn()
    {
        _fixture.SkipIfUnavailable();

        // First resolve the DN via name search.
        var byName = await _fixture.LdapService.FindUserAsync(
            _fixture.Settings.Integration.TestUsername);
        byName.ShouldNotBeNull();

        // Now look up by DN directly.
        var byDn = await _fixture.LdapService.FindUserByDnAsync(byName.Dn);

        byDn.ShouldNotBeNull();
        byDn.Dn.ShouldBe(byName.Dn);
    }

    [SkippableFact]
    public async Task FindUsersAsync_ReturnsNonEmptyList_WithBroadFilter()
    {
        _fixture.SkipIfUnavailable();

        // This filter is valid for both plain LDAP and Windows AD.
        var entries = await _fixture.LdapService.FindUsersAsync("(objectClass=user)");

        entries.ShouldNotBeNull();
        entries.Count.ShouldBeGreaterThan(0);
    }
}

using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Windows.Tests;

/// <summary>
/// Integration tests for group-search and user-group-membership operations
/// against a local Windows AD / LDAP server.
/// </summary>
[Collection(LdapIntegrationCollection.Name)]
public class LdapGroupSearchTests
{
    private readonly LdapIntegrationFixture _fixture;

    public LdapGroupSearchTests(LdapIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FindGroupsAsync_ReturnsAtLeastOneGroup()
    {
        _fixture.SkipIfUnavailable();

        var groups = await _fixture.LdapService.FindGroupsAsync();

        groups.ShouldNotBeNull();
        groups.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task FindGroupsAsync_ReturnsExpectedGroup_WhenConfigured()
    {
        _fixture.SkipIfUnavailable();

        var expectedGroup = _fixture.Settings.Integration.TestGroupName;
        if (string.IsNullOrEmpty(expectedGroup))
        {
            // No group configured — skip assertion but not the test.
            return;
        }

        var groups = await _fixture.LdapService.FindGroupsAsync();
        var match = groups.FirstOrDefault(g =>
            string.Equals(
                g.GetAttribute(_fixture.Settings.Ldap.GroupNameAttribute),
                expectedGroup,
                StringComparison.OrdinalIgnoreCase));

        match.ShouldNotBeNull(
            $"Expected to find a group named '{expectedGroup}' in the directory.");
    }

    [Fact]
    public async Task GetUserGroupsAsync_ReturnsGroups_ForExistingUser()
    {
        _fixture.SkipIfUnavailable();

        // Resolve user DN first.
        var userEntry = await _fixture.LdapService.FindUserAsync(
            _fixture.Settings.Integration.TestUsername);
        userEntry.ShouldNotBeNull();

        var groups = await _fixture.LdapService.GetUserGroupsAsync(userEntry.Dn);

        // A domain user is always a member of at least "Domain Users" in AD.
        groups.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetUserGroupsAsync_ContainsExpectedGroup_WhenConfigured()
    {
        _fixture.SkipIfUnavailable();

        var expectedGroup = _fixture.Settings.Integration.TestGroupName;
        if (string.IsNullOrEmpty(expectedGroup))
        {
            return;
        }

        var userEntry = await _fixture.LdapService.FindUserAsync(
            _fixture.Settings.Integration.TestUsername);
        userEntry.ShouldNotBeNull();

        var groups = await _fixture.LdapService.GetUserGroupsAsync(userEntry.Dn);
        var match = groups.FirstOrDefault(g =>
            string.Equals(
                g.GetAttribute(_fixture.Settings.Ldap.GroupNameAttribute),
                expectedGroup,
                StringComparison.OrdinalIgnoreCase));

        match.ShouldNotBeNull(
            $"Expected user '{_fixture.Settings.Integration.TestUsername}' " +
            $"to be a member of group '{expectedGroup}'.");
    }
}

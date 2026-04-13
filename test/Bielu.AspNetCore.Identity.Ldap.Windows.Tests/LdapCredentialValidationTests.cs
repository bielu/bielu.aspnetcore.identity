using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Windows.Tests;

/// <summary>
/// Integration tests for <see cref="Ldap.Abstractions.ILdapService.ValidateCredentialsAsync"/>.
/// These tests connect to the Windows AD / LDAP server configured in
/// <c>appsettings.Integration.json</c>.
/// </summary>
[Collection(LdapIntegrationCollection.Name)]
public class LdapCredentialValidationTests
{
    private readonly LdapIntegrationFixture _fixture;

    public LdapCredentialValidationTests(LdapIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ValidateCredentials_ReturnsTrue_ForValidUser()
    {
        _fixture.SkipIfUnavailable();

        var result = await _fixture.LdapService.ValidateCredentialsAsync(
            _fixture.Settings.Integration.TestUsername,
            _fixture.Settings.Integration.TestPassword);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateCredentials_ReturnsFalse_ForWrongPassword()
    {
        _fixture.SkipIfUnavailable();

        var result = await _fixture.LdapService.ValidateCredentialsAsync(
            _fixture.Settings.Integration.TestUsername,
            "definitely-wrong-password-that-will-never-match-1234");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateCredentials_ReturnsFalse_ForNonExistentUser()
    {
        _fixture.SkipIfUnavailable();

        var result = await _fixture.LdapService.ValidateCredentialsAsync(
            "user_does_not_exist_xyz_abc",
            "somepassword");

        result.ShouldBeFalse();
    }
}

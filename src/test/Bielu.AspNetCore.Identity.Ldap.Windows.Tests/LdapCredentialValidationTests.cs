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

    [SkippableFact]
    public async Task ValidateCredentials_ReturnsTrue_ForValidUser()
    {
        _fixture.SkipIfUnavailable();

        var result = await _fixture.LdapService.ValidateCredentialsAsync(
            _fixture.Settings.Integration.TestUsername,
            _fixture.Settings.Integration.TestPassword);

        result.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task ValidateCredentials_ReturnsFalse_ForWrongPassword()
    {
        _fixture.SkipIfUnavailable();

        // Use a dedicated account for bad-password tests to avoid incrementing
        // the bad-password counter and risking lockout on the primary test account.
        var badPwdUser = !string.IsNullOrEmpty(_fixture.Settings.Integration.BadPasswordTestUsername)
            ? _fixture.Settings.Integration.BadPasswordTestUsername
            : _fixture.Settings.Integration.TestUsername;

        var result = await _fixture.LdapService.ValidateCredentialsAsync(
            badPwdUser,
            "definitely-wrong-password-that-will-never-match-1234");

        result.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task ValidateCredentials_ReturnsFalse_ForNonExistentUser()
    {
        _fixture.SkipIfUnavailable();

        var result = await _fixture.LdapService.ValidateCredentialsAsync(
            "user_does_not_exist_xyz_abc",
            "somepassword");

        result.ShouldBeFalse();
    }
}

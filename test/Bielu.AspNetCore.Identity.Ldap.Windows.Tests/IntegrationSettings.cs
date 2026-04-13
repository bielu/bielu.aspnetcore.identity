namespace Bielu.AspNetCore.Identity.Ldap.Windows.Tests;

/// <summary>
/// Settings loaded from <c>appsettings.Integration.json</c> and environment variables.
/// </summary>
public sealed class IntegrationSettings
{
    /// <summary>LDAP connection options.</summary>
    public LdapOptions Ldap { get; set; } = new();

    /// <summary>Test-specific values (user credentials, expected results).</summary>
    public IntegrationTestValues Integration { get; set; } = new();
}

/// <summary>
/// Test-specific values used in the integration tests.
/// </summary>
public sealed class IntegrationTestValues
{
    /// <summary>Username of an existing user in the directory.</summary>
    public string TestUsername { get; set; } = string.Empty;

    /// <summary>Valid password for <see cref="TestUsername"/>.</summary>
    public string TestPassword { get; set; } = string.Empty;

    /// <summary>Expected e-mail address for <see cref="TestUsername"/>.</summary>
    public string TestUserEmail { get; set; } = string.Empty;

    /// <summary>Expected display name for <see cref="TestUsername"/>.</summary>
    public string TestUserDisplayName { get; set; } = string.Empty;

    /// <summary>Name of an existing group in the directory.</summary>
    public string TestGroupName { get; set; } = string.Empty;
}

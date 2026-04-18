using System.Diagnostics;
using System.Reflection;

namespace Bielu.AspNetCore.Identity.Ldap.OpenTelemetry;

/// <summary>
/// Provides the <see cref="ActivitySource"/> used for tracing LDAP identity operations.
/// </summary>
/// <remarks>
/// <para>
/// The attribute keys <see cref="AttributeUsername"/> and <see cref="AttributeUserDn"/>
/// may carry personally identifiable information (PII). They are recorded on
/// individual spans (request-scoped), not on metrics. Operators should evaluate
/// their data-retention policies and, if needed, configure their exporter to
/// redact or drop these attributes before sending telemetry to long-lived backends.
/// </para>
/// </remarks>
public static class LdapActivitySource
{
    private static readonly AssemblyName AssemblyName =
        typeof(LdapActivitySource).Assembly.GetName();

    /// <summary>
    /// The name of the activity source.
    /// </summary>
    public static readonly string Name = AssemblyName.Name!;

    /// <summary>
    /// The version of the activity source.
    /// </summary>
    public static readonly string Version = AssemblyName.Version?.ToString() ?? "0.0.0.0";

    /// <summary>
    /// The <see cref="ActivitySource"/> for all LDAP identity operations.
    /// </summary>
    public static readonly ActivitySource Source = new(Name, Version);

    // -----------------------------------------------------------------------
    // Operation names
    // -----------------------------------------------------------------------

    internal const string ValidateCredentials = "ldap.validate_credentials";
    internal const string FindUser = "ldap.find_user";
    internal const string FindUserByDn = "ldap.find_user_by_dn";
    internal const string FindUsers = "ldap.find_users";
    internal const string FindGroups = "ldap.find_groups";
    internal const string GetUserGroups = "ldap.get_user_groups";

    // -----------------------------------------------------------------------
    // Attribute keys
    // -----------------------------------------------------------------------

    internal const string AttributeLdapHost = "ldap.host";
    internal const string AttributeLdapPort = "ldap.port";
    internal const string AttributeUsername = "ldap.username";
    internal const string AttributeUserDn = "ldap.user_dn";
    internal const string AttributeResultCount = "ldap.result_count";
}

using System.Diagnostics.Metrics;
using System.Reflection;

namespace Bielu.AspNetCore.Identity.Ldap.OpenTelemetry;

/// <summary>
/// Provides the <see cref="Meter"/> used for recording LDAP identity metrics.
/// </summary>
public static class LdapMetrics
{
    private static readonly AssemblyName AssemblyName =
        typeof(LdapMetrics).Assembly.GetName();

    /// <summary>
    /// The name of the meter.
    /// </summary>
    public static readonly string Name = AssemblyName.Name!;

    /// <summary>
    /// The version of the meter.
    /// </summary>
    public static readonly string Version = AssemblyName.Version?.ToString() ?? "0.0.0.0";

    /// <summary>
    /// The <see cref="Meter"/> for all LDAP identity metrics.
    /// </summary>
    public static readonly Meter Meter = new(Name, Version);

    // -----------------------------------------------------------------------
    // Authentication metrics
    // -----------------------------------------------------------------------

    internal static readonly Counter<long> AuthenticationAttempts = Meter.CreateCounter<long>(
        "ldap.identity.authentication.attempts",
        description: "Total number of LDAP credential validation attempts");

    internal static readonly Counter<long> AuthenticationSuccesses = Meter.CreateCounter<long>(
        "ldap.identity.authentication.successes",
        description: "Total number of successful LDAP credential validations");

    internal static readonly Counter<long> AuthenticationFailures = Meter.CreateCounter<long>(
        "ldap.identity.authentication.failures",
        description: "Total number of failed LDAP credential validations");

    internal static readonly Histogram<double> AuthenticationDuration = Meter.CreateHistogram<double>(
        "ldap.identity.authentication.duration",
        unit: "s",
        description: "Duration of LDAP credential validation operations");

    // -----------------------------------------------------------------------
    // Search metrics
    // -----------------------------------------------------------------------

    internal static readonly Counter<long> SearchOperations = Meter.CreateCounter<long>(
        "ldap.identity.search.operations",
        description: "Total number of LDAP search operations");

    internal static readonly Histogram<double> SearchDuration = Meter.CreateHistogram<double>(
        "ldap.identity.search.duration",
        unit: "s",
        description: "Duration of LDAP search operations");

    // -----------------------------------------------------------------------
    // Error metrics
    // -----------------------------------------------------------------------

    internal static readonly Counter<long> OperationErrors = Meter.CreateCounter<long>(
        "ldap.identity.operation.errors",
        description: "Total number of LDAP operation errors");
}

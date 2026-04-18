using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.OpenTelemetry.Instrumentation;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Bielu.AspNetCore.Identity.Ldap.OpenTelemetry.Extensions;

/// <summary>
/// Extension methods for adding OpenTelemetry instrumentation to the LDAP identity provider.
/// </summary>
public static class LdapOpenTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Wraps the registered <see cref="ILdapService"/> with the
    /// <see cref="OpenTelemetryLdapServiceDecorator"/> so that every LDAP operation
    /// is automatically traced and metered.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddLdapOpenTelemetryInstrumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Replace the existing ILdapService registration with the decorator.
        services.Decorate<ILdapService, OpenTelemetryLdapServiceDecorator>();

        return services;
    }

    /// <summary>
    /// Registers the LDAP identity <see cref="System.Diagnostics.ActivitySource"/> with the
    /// OpenTelemetry <see cref="TracerProviderBuilder"/> so spans are exported.
    /// </summary>
    /// <param name="builder">The tracer provider builder.</param>
    /// <returns>The tracer provider builder for chaining.</returns>
    public static TracerProviderBuilder AddLdapIdentityInstrumentation(this TracerProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddSource(LdapActivitySource.Name);
    }

    /// <summary>
    /// Registers the LDAP identity <see cref="System.Diagnostics.Metrics.Meter"/> with the
    /// OpenTelemetry <see cref="MeterProviderBuilder"/> so metrics are exported.
    /// </summary>
    /// <param name="builder">The meter provider builder.</param>
    /// <returns>The meter provider builder for chaining.</returns>
    public static MeterProviderBuilder AddLdapIdentityMetrics(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddMeter(LdapMetrics.Name);
    }
}

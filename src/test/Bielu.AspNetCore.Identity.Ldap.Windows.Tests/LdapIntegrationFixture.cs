using System.DirectoryServices.Protocols;
using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Windows.Tests;

/// <summary>
/// xUnit class fixture that:
/// <list type="bullet">
///   <item>Loads settings from <c>appsettings.Integration.json</c> and environment variables.</item>
///   <item>Checks whether the LDAP server is reachable.</item>
///   <item>Exposes an <see cref="ILdapService"/> and <see cref="IntegrationSettings"/> to tests.</item>
/// </list>
/// All integration tests are skipped automatically when the server is unreachable or
/// when the opt-in environment variable <c>LDAP_INTEGRATION=true</c> is not set.
/// </summary>
public sealed class LdapIntegrationFixture : IDisposable
{
    private readonly ServiceProvider _provider;

    /// <summary>
    /// Set to <c>true</c> when the LDAP server is reachable and the opt-in flag is set.
    /// When <c>false</c> every test using this fixture should call
    /// <see cref="SkipIfUnavailable"/> to be skipped.
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>Settings loaded from configuration.</summary>
    public IntegrationSettings Settings { get; }

    /// <summary>The shared <see cref="IConfiguration"/> instance.</summary>
    public IConfiguration Configuration { get; }

    /// <summary>The LDAP service instance under test.</summary>
    public ILdapService LdapService { get; }

    public LdapIntegrationFixture()
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Integration.json", optional: true)
            // Environment-variable overrides use the standard __ separator for
            // nesting, e.g. Ldap__Host=myserver  →  Ldap:Host=myserver
            .AddEnvironmentVariables()
            .Build();

        Settings = new IntegrationSettings();
        Configuration.Bind(Settings);
        Configuration.GetSection("Integration").Bind(Settings.Integration);

        // Build the DI container using the IConfiguration overload so the fixture
        // exercises the real registration path used by production apps.
        var services = new ServiceCollection();
        services.AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Use the IConfiguration overload with the default section name constant.
        services.AddLdapIdentity(Configuration, LdapOptions.SectionName);

        _provider = services.BuildServiceProvider();
        LdapService = _provider.GetRequiredService<ILdapService>();

        // Opt-in guard: set LDAP_INTEGRATION=true to run these tests.
        var optIn = Environment.GetEnvironmentVariable("LDAP_INTEGRATION");
        if (!string.Equals(optIn, "true", StringComparison.OrdinalIgnoreCase))
        {
            IsAvailable = false;
            return;
        }

        IsAvailable = IsServerReachable(Settings.Ldap);
    }

    /// <summary>
    /// Marks the current test as skipped (via <see cref="Skip"/>) when the LDAP
    /// server is unavailable, so it is reported as skipped rather than failed.
    /// Only effective on tests annotated with <c>[SkippableFact]</c>/<c>[SkippableTheory]</c>.
    /// </summary>
    public void SkipIfUnavailable()
        => Skip.IfNot(
            IsAvailable,
            "LDAP integration tests are skipped. " +
            "Set the environment variable LDAP_INTEGRATION=true and ensure the LDAP server " +
            $"configured under '{LdapOptions.SectionName}' in appsettings.Integration.json " +
            "(or via Ldap__* env vars) is reachable.");

    /// <inheritdoc/>
    public void Dispose() => _provider.Dispose();

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static bool IsServerReachable(LdapOptions options)
    {
        try
        {
            var identifier = new LdapDirectoryIdentifier(options.Host, options.Port);
            using var connection = new LdapConnection(identifier)
            {
                Timeout = TimeSpan.FromSeconds(5),
            };

            if (options.UsesSsl)
            {
                connection.SessionOptions.SecureSocketLayer = true;
            }

            connection.SessionOptions.ProtocolVersion = 3;
            connection.Bind(); // anonymous bind — just checks connectivity
            return true;
        }
        catch
        {
            return false;
        }
    }
}

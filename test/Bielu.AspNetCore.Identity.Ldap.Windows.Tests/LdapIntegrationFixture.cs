using System.DirectoryServices.Protocols;
using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Bielu.AspNetCore.Identity.Ldap.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            // Environment-variable overrides: prefix LDAP_ maps to the "Ldap" section.
            // e.g. LDAP_Host=myserver  →  Ldap:Host=myserver
            .AddEnvironmentVariables("LDAP_")
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
    /// Throws <see cref="SkipException"/> when the LDAP server is unavailable
    /// so the test is marked as skipped rather than failed.
    /// </summary>
    public void SkipIfUnavailable()
    {
        if (!IsAvailable)
        {
            throw new SkipException(
                "LDAP integration tests are skipped. " +
                "Set the environment variable LDAP_INTEGRATION=true and ensure the LDAP server " +
                $"configured under '{LdapOptions.SectionName}' in appsettings.Integration.json " +
                "(or via LDAP_ env vars) is reachable.");
        }
    }

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
            connection.Bind(); // anonymous bind — just checks TCP connectivity
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Thrown by <see cref="LdapIntegrationFixture.SkipIfUnavailable"/> to signal xUnit
/// that a test should be skipped.
/// </summary>
public sealed class SkipException : Exception
{
    /// <inheritdoc/>
    public SkipException(string message) : base(message) { }
}

using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Bielu.AspNetCore.Identity.Ldap.OpenTelemetry.Extensions;
using Bielu.AspNetCore.Identity.Ldap.OpenTelemetry.Instrumentation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Tests.Unit;

public class ServiceRegistrationTests
{
    // -----------------------------------------------------------------------
    // Fluent API overload  (Action<LdapOptions>)
    // -----------------------------------------------------------------------

    [Fact]
    public void AddLdapIdentity_FluentApi_RegistersILdapService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddLdapIdentity(options =>
        {
            options.Host = "localhost";
            options.UserSearchBase = "ou=users,dc=example,dc=com";
        });

        using var provider = services.BuildServiceProvider();
        var ldapService = provider.GetService<ILdapService>();

        ldapService.ShouldNotBeNull();
    }

    [Fact]
    public void AddLdapIdentity_FluentApi_OptionsAreApplied()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddLdapIdentity(options =>
        {
            options.Host = "my-ldap-server";
            options.Port = 636;
            options.UsesSsl = true;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LdapOptions>>().Value;

        options.Host.ShouldBe("my-ldap-server");
        options.Port.ShouldBe(636);
        options.UsesSsl.ShouldBeTrue();
    }

    [Fact]
    public void AddLdapIdentity_FluentApi_ThrowsOnNullServices()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddLdapIdentity(_ => { }));
    }

    [Fact]
    public void AddLdapIdentity_FluentApi_ThrowsOnNullDelegate()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddLdapIdentity((Action<LdapOptions>)null!));
    }

    // -----------------------------------------------------------------------
    // IConfiguration overload
    // -----------------------------------------------------------------------

    [Fact]
    public void AddLdapIdentity_Configuration_RegistersILdapService()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ldap:Host"] = "config-ldap-server",
            ["Ldap:Port"] = "389",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLdapIdentity(configuration);

        using var provider = services.BuildServiceProvider();
        var ldapService = provider.GetService<ILdapService>();

        ldapService.ShouldNotBeNull();
    }

    [Fact]
    public void AddLdapIdentity_Configuration_BindsOptionsFromDefaultSection()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            [$"{LdapOptions.SectionName}:Host"] = "config-ldap-server",
            [$"{LdapOptions.SectionName}:Port"] = "636",
            [$"{LdapOptions.SectionName}:UsesSsl"] = "true",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLdapIdentity(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LdapOptions>>().Value;

        options.Host.ShouldBe("config-ldap-server");
        options.Port.ShouldBe(636);
        options.UsesSsl.ShouldBeTrue();
    }

    [Fact]
    public void AddLdapIdentity_Configuration_BindsOptionsFromCustomSection()
    {
        const string customSection = "MyApp:Directory";

        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["MyApp:Directory:Host"] = "custom-section-server",
            ["MyApp:Directory:Port"] = "3268",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLdapIdentity(configuration, sectionName: customSection);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LdapOptions>>().Value;

        options.Host.ShouldBe("custom-section-server");
        options.Port.ShouldBe(3268);
    }

    [Fact]
    public void AddLdapIdentity_Configuration_ThrowsOnNullServices()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddLdapIdentity(configuration));
    }

    [Fact]
    public void AddLdapIdentity_Configuration_ThrowsOnNullConfiguration()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddLdapIdentity((IConfiguration)null!));
    }

    // -----------------------------------------------------------------------
    // SectionName constant
    // -----------------------------------------------------------------------

    [Fact]
    public void LdapOptions_SectionName_IsLdap()
    {
        LdapOptions.SectionName.ShouldBe("Ldap");
    }

    // -----------------------------------------------------------------------
    // IdentityBuilder extension: AddLdapStores (builds on top of AddIdentity)
    // -----------------------------------------------------------------------

    [Fact]
    public void AddLdapStores_Configuration_RegistersILdapService()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ldap:Host"] = "stores-ldap-server",
            ["Ldap:Port"] = "389",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentity<LdapUser, LdapRole>()
            .AddLdapStores(configuration);

        using var provider = services.BuildServiceProvider();
        var ldapService = provider.GetService<ILdapService>();

        ldapService.ShouldNotBeNull();
    }

    [Fact]
    public void AddLdapStores_Configuration_BindsOptions()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ldap:Host"] = "stores-ldap-server",
            ["Ldap:Port"] = "636",
            ["Ldap:UsesSsl"] = "true",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentity<LdapUser, LdapRole>()
            .AddLdapStores(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LdapOptions>>().Value;

        options.Host.ShouldBe("stores-ldap-server");
        options.Port.ShouldBe(636);
        options.UsesSsl.ShouldBeTrue();
    }

    [Fact]
    public void AddLdapStores_Configuration_CustomSection()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Custom:Host"] = "custom-server",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentity<LdapUser, LdapRole>()
            .AddLdapStores(configuration, sectionName: "Custom");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LdapOptions>>().Value;

        options.Host.ShouldBe("custom-server");
    }

    [Fact]
    public void AddLdapStores_FluentApi_RegistersILdapService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentity<LdapUser, LdapRole>()
            .AddLdapStores(options =>
            {
                options.Host = "stores-fluent-server";
            });

        using var provider = services.BuildServiceProvider();
        var ldapService = provider.GetService<ILdapService>();

        ldapService.ShouldNotBeNull();
    }

    [Fact]
    public void AddLdapStores_FluentApi_OptionsAreApplied()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentity<LdapUser, LdapRole>()
            .AddLdapStores(options =>
            {
                options.Host = "stores-fluent-server";
                options.Port = 636;
            });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LdapOptions>>().Value;

        options.Host.ShouldBe("stores-fluent-server");
        options.Port.ShouldBe(636);
    }

    [Fact]
    public void AddLdapStores_ThrowsOnNullBuilder()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        Should.Throw<ArgumentNullException>(() =>
            ((IdentityBuilder)null!).AddLdapStores(configuration));
    }

    [Fact]
    public void AddLdapStores_FluentApi_ThrowsOnNullBuilder()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IdentityBuilder)null!).AddLdapStores(_ => { }));
    }

    // -----------------------------------------------------------------------
    // Service-only: AddLdapServices (for mixed EF + LDAP scenarios)
    // -----------------------------------------------------------------------

    [Fact]
    public void AddLdapServices_Configuration_RegistersILdapService()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ldap:Host"] = "services-ldap-server",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLdapServices(configuration);

        using var provider = services.BuildServiceProvider();
        var ldapService = provider.GetService<ILdapService>();

        ldapService.ShouldNotBeNull();
    }

    [Fact]
    public void AddLdapServices_Configuration_DoesNotRegisterStores()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ldap:Host"] = "services-ldap-server",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLdapServices(configuration);

        // IUserStore and IRoleStore should NOT be registered by AddLdapServices
        services.ShouldNotContain(s => s.ServiceType == typeof(IUserStore<LdapUser>));
        services.ShouldNotContain(s => s.ServiceType == typeof(IRoleStore<LdapRole>));
    }

    [Fact]
    public void AddLdapServices_FluentApi_RegistersILdapService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLdapServices(options =>
        {
            options.Host = "services-fluent-server";
        });

        using var provider = services.BuildServiceProvider();
        var ldapService = provider.GetService<ILdapService>();

        ldapService.ShouldNotBeNull();
    }

    [Fact]
    public void AddLdapServices_FluentApi_OptionsAreApplied()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLdapServices(options =>
        {
            options.Host = "services-fluent-server";
            options.Port = 636;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LdapOptions>>().Value;

        options.Host.ShouldBe("services-fluent-server");
        options.Port.ShouldBe(636);
    }

    [Fact]
    public void AddLdapServices_Configuration_ThrowsOnNullServices()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddLdapServices(configuration));
    }

    [Fact]
    public void AddLdapServices_FluentApi_ThrowsOnNullServices()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddLdapServices(_ => { }));
    }

    [Fact]
    public void AddLdapServices_FluentApi_ThrowsOnNullDelegate()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddLdapServices((Action<LdapOptions>)null!));
    }

    [Fact]
    public void AddLdapServices_DoesNotDuplicateRegistration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLdapServices(options => { options.Host = "first"; });
        services.AddLdapServices(options => { options.Host = "second"; });

        // ILdapService should only be registered once
        services.Count(s => s.ServiceType == typeof(ILdapService)).ShouldBe(1);
    }

    // -----------------------------------------------------------------------
    // OpenTelemetry decorator
    // -----------------------------------------------------------------------

    [Fact]
    public void AddLdapOpenTelemetryInstrumentation_WrapsLdapService_FluentApi()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddLdapIdentity(options =>
        {
            options.Host = "localhost";
            options.UserSearchBase = "ou=users,dc=example,dc=com";
        });

        services.AddLdapOpenTelemetryInstrumentation();

        using var provider = services.BuildServiceProvider();
        var ldapService = provider.GetRequiredService<ILdapService>();

        ldapService.ShouldBeOfType<OpenTelemetryLdapServiceDecorator>();
    }

    [Fact]
    public void AddLdapOpenTelemetryInstrumentation_WrapsLdapService_Configuration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Ldap:Host"] = "localhost",
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLdapIdentity(configuration);
        services.AddLdapOpenTelemetryInstrumentation();

        using var provider = services.BuildServiceProvider();
        var ldapService = provider.GetRequiredService<ILdapService>();

        ldapService.ShouldBeOfType<OpenTelemetryLdapServiceDecorator>();
    }

    [Fact]
    public void AddLdapOpenTelemetryInstrumentation_ThrowsOnNullServices()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddLdapOpenTelemetryInstrumentation());
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}

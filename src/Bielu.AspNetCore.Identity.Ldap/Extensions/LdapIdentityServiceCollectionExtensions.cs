using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Internal;
using Bielu.AspNetCore.Identity.Ldap.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bielu.AspNetCore.Identity.Ldap.Extensions;

/// <summary>
/// Extension methods for registering the LDAP identity provider in the DI container.
/// </summary>
public static class LdapIdentityServiceCollectionExtensions
{
    // -----------------------------------------------------------------------
    // Convenience: AddIdentity + AddLdapStores in one call
    // -----------------------------------------------------------------------

    /// <summary>
    /// Convenience method that calls
    /// <see cref="IdentityServiceCollectionExtensions.AddIdentity{TUser, TRole}(IServiceCollection)"/>
    /// followed by <see cref="AddLdapStores(IdentityBuilder, IConfiguration, string)"/>.
    /// Use this when LDAP is the <b>only</b> identity store.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">
    /// The configuration section key to bind. Defaults to <see cref="LdapOptions.SectionName"/>
    /// (<c>"Ldap"</c>).
    /// </param>
    /// <returns>An <see cref="IdentityBuilder"/> for further Identity configuration.</returns>
    public static IdentityBuilder AddLdapIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = LdapOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);

        return services
            .AddIdentity<LdapUser, LdapRole>()
            .AddLdapStores(configuration, sectionName);
    }

    /// <summary>
    /// Convenience method that calls
    /// <see cref="IdentityServiceCollectionExtensions.AddIdentity{TUser, TRole}(IServiceCollection)"/>
    /// followed by <see cref="AddLdapStores(IdentityBuilder, Action{LdapOptions})"/>.
    /// Use this when LDAP is the <b>only</b> identity store.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="LdapOptions"/>.</param>
    /// <returns>An <see cref="IdentityBuilder"/> for further Identity configuration.</returns>
    public static IdentityBuilder AddLdapIdentity(
        this IServiceCollection services,
        Action<LdapOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services
            .AddIdentity<LdapUser, LdapRole>()
            .AddLdapStores(configureOptions);
    }

    // -----------------------------------------------------------------------
    // IdentityBuilder extensions: AddLdapStores
    // -----------------------------------------------------------------------

    /// <summary>
    /// Adds the LDAP-backed <see cref="IUserStore{TUser}"/> and
    /// <see cref="IRoleStore{TRole}"/> to an existing <see cref="IdentityBuilder"/>,
    /// binding <see cref="LdapOptions"/> from the given <paramref name="configuration"/> section.
    /// </summary>
    /// <param name="builder">The <see cref="IdentityBuilder"/> to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">
    /// The configuration section key to bind. Defaults to <see cref="LdapOptions.SectionName"/>
    /// (<c>"Ldap"</c>).
    /// </param>
    /// <returns>The same <see cref="IdentityBuilder"/> for chaining.</returns>
    public static IdentityBuilder AddLdapStores(
        this IdentityBuilder builder,
        IConfiguration configuration,
        string sectionName = LdapOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);

        builder.Services.AddLdapServices(configuration, sectionName);
        RegisterStores(builder);

        return builder;
    }

    /// <summary>
    /// Adds the LDAP-backed <see cref="IUserStore{TUser}"/> and
    /// <see cref="IRoleStore{TRole}"/> to an existing <see cref="IdentityBuilder"/>,
    /// configuring <see cref="LdapOptions"/> from the supplied <paramref name="configureOptions"/> delegate.
    /// </summary>
    /// <param name="builder">The <see cref="IdentityBuilder"/> to configure.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="LdapOptions"/>.</param>
    /// <returns>The same <see cref="IdentityBuilder"/> for chaining.</returns>
    public static IdentityBuilder AddLdapStores(
        this IdentityBuilder builder,
        Action<LdapOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services.AddLdapServices(configureOptions);
        RegisterStores(builder);

        return builder;
    }

    // -----------------------------------------------------------------------
    // Service-only registration: AddLdapServices
    // -----------------------------------------------------------------------

    /// <summary>
    /// Registers <see cref="LdapOptions"/> and <see cref="ILdapService"/> <b>without</b>
    /// registering Identity stores. Use this in mixed-store scenarios where Entity Framework
    /// (or another provider) owns the Identity stores and LDAP is used only for credential
    /// validation via <see cref="ILdapService"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">
    /// The configuration section key to bind. Defaults to <see cref="LdapOptions.SectionName"/>
    /// (<c>"Ldap"</c>).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddLdapServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = LdapOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);

        services.Configure<LdapOptions>(configuration.GetSection(sectionName));
        services.TryAddLdapService();

        return services;
    }

    /// <summary>
    /// Registers <see cref="LdapOptions"/> and <see cref="ILdapService"/> <b>without</b>
    /// registering Identity stores. Use this in mixed-store scenarios where Entity Framework
    /// (or another provider) owns the Identity stores and LDAP is used only for credential
    /// validation via <see cref="ILdapService"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="LdapOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddLdapServices(
        this IServiceCollection services,
        Action<LdapOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.TryAddLdapService();

        return services;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static void TryAddLdapService(this IServiceCollection services)
    {
        // Avoid duplicate registration when both AddLdapServices and AddLdapStores are called.
        if (!services.Any(s => s.ServiceType == typeof(ILdapService)))
        {
            services.AddSingleton<ILdapService, LdapService>();
        }
    }

    private static void RegisterStores(IdentityBuilder builder)
    {
        builder.Services.AddTransient<IUserStore<LdapUser>, LdapUserStore>();
        builder.Services.AddTransient<IRoleStore<LdapRole>, LdapRoleStore>();
    }
}

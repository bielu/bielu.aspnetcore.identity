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
    /// <summary>
    /// Registers the LDAP identity services and binds <see cref="LdapOptions"/> from the
    /// <paramref name="configuration"/> section identified by <paramref name="sectionName"/>.
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

        services.Configure<LdapOptions>(configuration.GetSection(sectionName));
        services.AddSingleton<ILdapService, LdapService>();

        return services
            .AddIdentity<LdapUser, LdapRole>()
            .AddLdapStores();
    }

    /// <summary>
    /// Registers the LDAP identity services and configures <see cref="LdapOptions"/>
    /// from the supplied <paramref name="configureOptions"/> delegate.
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

        services.Configure(configureOptions);
        services.AddSingleton<ILdapService, LdapService>();

        return services
            .AddIdentity<LdapUser, LdapRole>()
            .AddLdapStores();
    }

    /// <summary>
    /// Adds the LDAP-backed <see cref="IUserStore{TUser}"/> and
    /// <see cref="IRoleStore{TRole}"/> to an existing <see cref="IdentityBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IdentityBuilder"/> to configure.</param>
    /// <returns>The same <see cref="IdentityBuilder"/> for chaining.</returns>
    public static IdentityBuilder AddLdapStores(this IdentityBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddTransient<IUserStore<LdapUser>, LdapUserStore>();
        builder.Services.AddTransient<IRoleStore<LdapRole>, LdapRoleStore>();

        return builder;
    }
}

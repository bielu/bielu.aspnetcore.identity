namespace Bielu.AspNetCore.Identity.Ldap;

/// <summary>
/// Configuration options for the LDAP identity provider.
/// </summary>
public sealed class LdapOptions
{
    /// <summary>
    /// The default configuration section key used when binding from <c>appsettings.json</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// // appsettings.json
    /// {
    ///   "Ldap": {
    ///     "Host": "ldap.example.com",
    ///     "Port": 389
    ///   }
    /// }
    /// </code>
    /// Override via the <c>sectionName</c> parameter on
    /// <see cref="Extensions.LdapIdentityServiceCollectionExtensions.AddLdapIdentity(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration, string)"/>.
    /// </example>
    public const string SectionName = "Ldap";
    /// <summary>
    /// The LDAP server hostname or IP address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// The LDAP server port. Defaults to <c>389</c> (plain) or <c>636</c> (SSL).
    /// </summary>
    public int Port { get; set; } = 389;

    /// <summary>
    /// Whether to use SSL/TLS when connecting to the LDAP server.
    /// When <c>true</c> the default port changes to <c>636</c>.
    /// </summary>
    public bool UsesSsl { get; set; } = false;

    /// <summary>
    /// The Distinguished Name used to bind to the LDAP directory for search operations
    /// (a.k.a. the service-account DN). Leave <c>null</c> for anonymous bind.
    /// </summary>
    public string? BindDn { get; set; }

    /// <summary>
    /// The password for the <see cref="BindDn"/> service account.
    /// </summary>
    public string? BindPassword { get; set; }

    /// <summary>
    /// The base DN under which users are searched.
    /// </summary>
    public string UserSearchBase { get; set; } = string.Empty;

    /// <summary>
    /// The LDAP search filter used to locate a user. The placeholder <c>{0}</c> is
    /// replaced with the normalized username at runtime.
    /// Defaults to <c>(&amp;(objectClass=person)(uid={0}))</c>.
    /// </summary>
    public string UserSearchFilter { get; set; } = "(&(objectClass=person)(uid={0}))";

    /// <summary>
    /// The base DN under which groups/roles are searched.
    /// </summary>
    public string GroupSearchBase { get; set; } = string.Empty;

    /// <summary>
    /// The LDAP search filter used to find groups that a user is a member of.
    /// The placeholder <c>{0}</c> is replaced with the user DN at runtime.
    /// Defaults to <c>(&amp;(objectClass=groupOfNames)(member={0}))</c>.
    /// </summary>
    public string GroupSearchFilter { get; set; } = "(&(objectClass=groupOfNames)(member={0}))";

    /// <summary>
    /// The LDAP attribute that holds the user's e-mail address.
    /// Defaults to <c>mail</c>.
    /// </summary>
    public string EmailAttribute { get; set; } = "mail";

    /// <summary>
    /// The LDAP attribute that holds the username (login name).
    /// Defaults to <c>uid</c>.
    /// </summary>
    public string UsernameAttribute { get; set; } = "uid";

    /// <summary>
    /// The LDAP attribute that holds the user's display name.
    /// Defaults to <c>cn</c>.
    /// </summary>
    public string DisplayNameAttribute { get; set; } = "cn";

    /// <summary>
    /// The LDAP attribute that holds the group's common name / role name.
    /// Defaults to <c>cn</c>.
    /// </summary>
    public string GroupNameAttribute { get; set; } = "cn";

    /// <summary>
    /// Connection timeout in seconds.  Defaults to <c>30</c>.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}

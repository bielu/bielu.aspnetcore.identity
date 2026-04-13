using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Windows.Tests;

/// <summary>
/// xUnit collection definition that shares a single <see cref="LdapIntegrationFixture"/>
/// across all Windows LDAP integration test classes, avoiding repeated LDAP connections.
/// </summary>
[CollectionDefinition(Name)]
public sealed class LdapIntegrationCollection : ICollectionFixture<LdapIntegrationFixture>
{
    /// <summary>The collection name used in <c>[Collection]</c> attributes.</summary>
    public const string Name = "LdapIntegration";
}

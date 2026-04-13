using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Tests.Unit;

public class LdapEntryTests
{
    [Fact]
    public void LdapEntry_GetAttribute_ReturnsFirstValue()
    {
        var entry = new LdapEntry
        {
            Dn = "cn=jdoe,ou=users,dc=example,dc=com",
            Attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["uid"] = new[] { "jdoe" },
                ["mail"] = new[] { "jdoe@example.com" },
                ["cn"] = new[] { "John Doe", "J. Doe" },
            },
        };

        entry.GetAttribute("uid").ShouldBe("jdoe");
        entry.GetAttribute("mail").ShouldBe("jdoe@example.com");
        entry.GetAttribute("cn").ShouldBe("John Doe");
    }

    [Fact]
    public void LdapEntry_GetAttribute_IsCaseInsensitive()
    {
        var entry = new LdapEntry
        {
            Dn = "cn=jdoe,ou=users,dc=example,dc=com",
            Attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["sAMAccountName"] = new[] { "jdoe" },
            },
        };

        entry.GetAttribute("samaccountname").ShouldBe("jdoe");
        entry.GetAttribute("SAMACCOUNTNAME").ShouldBe("jdoe");
        entry.GetAttribute("sAMAccountName").ShouldBe("jdoe");
    }

    [Fact]
    public void LdapEntry_GetAttribute_ReturnsNullWhenAttributeMissing()
    {
        var entry = new LdapEntry
        {
            Dn = "cn=jdoe,ou=users,dc=example,dc=com",
            Attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase),
        };

        entry.GetAttribute("uid").ShouldBeNull();
    }

    [Fact]
    public void LdapEntry_GetAttribute_ThrowsOnNullName()
    {
        var entry = new LdapEntry();
        Should.Throw<ArgumentNullException>(() => entry.GetAttribute(null!));
    }

    [Fact]
    public void LdapEntry_DefaultAttributes_IsEmptyDictionary()
    {
        var entry = new LdapEntry();
        entry.Attributes.ShouldNotBeNull();
        entry.Attributes.ShouldBeEmpty();
    }
}

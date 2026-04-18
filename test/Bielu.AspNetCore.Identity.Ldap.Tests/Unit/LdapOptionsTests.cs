using Bielu.AspNetCore.Identity.Ldap;
using Shouldly;
using Xunit;

namespace Bielu.AspNetCore.Identity.Ldap.Tests.Unit;

public class LdapOptionsTests
{
    [Fact]
    public void LdapOptions_DefaultValues_AreCorrect()
    {
        var options = new LdapOptions();

        options.Host.ShouldBe("localhost");
        options.Port.ShouldBe(389);
        options.UsesSsl.ShouldBeFalse();
        options.BindDn.ShouldBeNull();
        options.BindPassword.ShouldBeNull();
        options.UserSearchBase.ShouldBe(string.Empty);
        options.UserSearchFilter.ShouldBe("(&(objectClass=person)(uid={0}))");
        options.GroupSearchBase.ShouldBe(string.Empty);
        options.GroupSearchFilter.ShouldBe("(&(objectClass=groupOfNames)(member={0}))");
        options.GroupListFilter.ShouldBe("(objectClass=groupOfNames)");
        options.EmailAttribute.ShouldBe("mail");
        options.UsernameAttribute.ShouldBe("uid");
        options.DisplayNameAttribute.ShouldBe("cn");
        options.GroupNameAttribute.ShouldBe("cn");
        options.ConnectionTimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    public void LdapOptions_CanBeConfigured()
    {
        var options = new LdapOptions
        {
            Host = "ldap.example.com",
            Port = 636,
            UsesSsl = true,
            BindDn = "cn=admin,dc=example,dc=com",
            BindPassword = "s3cr3t",
            UserSearchBase = "ou=users,dc=example,dc=com",
            UserSearchFilter = "(&(objectClass=inetOrgPerson)(uid={0}))",
            GroupSearchBase = "ou=groups,dc=example,dc=com",
            EmailAttribute = "mail",
            UsernameAttribute = "uid",
            DisplayNameAttribute = "displayName",
            GroupNameAttribute = "cn",
            ConnectionTimeoutSeconds = 60,
        };

        options.Host.ShouldBe("ldap.example.com");
        options.Port.ShouldBe(636);
        options.UsesSsl.ShouldBeTrue();
        options.BindDn.ShouldBe("cn=admin,dc=example,dc=com");
        options.BindPassword.ShouldBe("s3cr3t");
        options.UserSearchBase.ShouldBe("ou=users,dc=example,dc=com");
        options.GroupSearchBase.ShouldBe("ou=groups,dc=example,dc=com");
        options.EmailAttribute.ShouldBe("mail");
        options.UsernameAttribute.ShouldBe("uid");
        options.DisplayNameAttribute.ShouldBe("displayName");
        options.ConnectionTimeoutSeconds.ShouldBe(60);
    }
}

# Bielu.AspNetCore.Identity.Ldap

ASP.NET Core Identity provider for **LDAP** directories — authenticate users and resolve roles directly against any LDAP-compatible server (OpenLDAP, Active Directory, 389 Directory Server, etc.) without a local database.

Built on [`System.DirectoryServices.Protocols`](https://learn.microsoft.com/dotnet/api/system.directoryservices.protocols) for **cross-platform** support (Windows, Linux, macOS) — no Novell dependency required.

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## Features

- **ASP.NET Core Identity stores** — drop-in `IUserStore<LdapUser>` and `IRoleStore<LdapRole>` implementations.
- **Two registration APIs** — bind options from `IConfiguration` (appsettings) **or** use a fluent `Action<LdapOptions>` delegate.
- **Overridable configuration section key** — defaults to `"Ldap"` (`LdapOptions.SectionName`), but callers can pass any section name.
- **OpenTelemetry instrumentation** — optional package adds tracing spans and metrics counters/histograms to every LDAP operation.
- **Cross-platform** — uses `System.DirectoryServices.Protocols` (no Windows-only COM dependencies).
- **IOptionsMonitor** — supports hot-reload of LDAP settings when using configuration providers that support change notifications.
- **Static analysis** — uses `Bielu.StaticCode.Analyzers`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`, and `Microsoft.VisualStudio.Threading.Analyzers`.

---

## Packages

| Package | Description |
|---|---|
| `Bielu.AspNetCore.Identity.Ldap` | Core identity stores, `ILdapService`, models, and DI registration |
| `Bielu.AspNetCore.Identity.Ldap.OpenTelemetry` | OpenTelemetry tracing & metrics decorator for LDAP operations |

---

## Installation

```bash
# Core package
dotnet add package Bielu.AspNetCore.Identity.Ldap

# (Optional) OpenTelemetry instrumentation
dotnet add package Bielu.AspNetCore.Identity.Ldap.OpenTelemetry
```

---

## Quick Start

### Option 1 — Bind from `appsettings.json` (recommended)

Add the LDAP section to your `appsettings.json`:

```json
{
  "Ldap": {
    "Host": "ldap.example.com",
    "Port": 389,
    "UsesSsl": false,
    "BindDn": "cn=readonly,dc=example,dc=com",
    "BindPassword": "secret",
    "UserSearchBase": "ou=People,dc=example,dc=com",
    "UserSearchFilter": "(&(objectClass=person)(uid={0}))",
    "GroupSearchBase": "ou=Groups,dc=example,dc=com",
    "GroupSearchFilter": "(&(objectClass=groupOfNames)(member={0}))",
    "EmailAttribute": "mail",
    "UsernameAttribute": "uid",
    "DisplayNameAttribute": "cn",
    "GroupNameAttribute": "cn",
    "ConnectionTimeoutSeconds": 30
  }
}
```

Register the LDAP identity provider in `Program.cs`:

```csharp
using Bielu.AspNetCore.Identity.Ldap.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Reads from the "Ldap" section by default (LdapOptions.SectionName)
builder.Services.AddLdapIdentity(builder.Configuration);
```

You can also override the configuration section key:

```csharp
// Bind from a custom section path
builder.Services.AddLdapIdentity(builder.Configuration, sectionName: "MyApp:Directory");
```

### Option 2 — Fluent API

```csharp
using Bielu.AspNetCore.Identity.Ldap.Extensions;

builder.Services.AddLdapIdentity(options =>
{
    options.Host = "ldap.example.com";
    options.Port = 636;
    options.UsesSsl = true;
    options.BindDn = "cn=readonly,dc=example,dc=com";
    options.BindPassword = "secret";
    options.UserSearchBase = "ou=People,dc=example,dc=com";
    options.UserSearchFilter = "(&(objectClass=person)(uid={0}))";
    options.GroupSearchBase = "ou=Groups,dc=example,dc=com";
    options.GroupSearchFilter = "(&(objectClass=groupOfNames)(member={0}))";
});
```

---

## Configuration Reference

All properties live on `LdapOptions`:

| Property | Type | Default | Description |
|---|---|---|---|
| `Host` | `string` | `"localhost"` | LDAP server hostname or IP |
| `Port` | `int` | `389` | LDAP port (`389` plain, `636` SSL) |
| `UsesSsl` | `bool` | `false` | Use SSL/TLS |
| `BindDn` | `string?` | `null` | Service-account DN for search operations (anonymous if null) |
| `BindPassword` | `string?` | `null` | Password for `BindDn` |
| `UserSearchBase` | `string` | `""` | Base DN for user searches |
| `UserSearchFilter` | `string` | `"(&(objectClass=person)(uid={0}))"` | Filter template — `{0}` = username |
| `GroupSearchBase` | `string` | `""` | Base DN for group searches |
| `GroupSearchFilter` | `string` | `"(&(objectClass=groupOfNames)(member={0}))"` | Filter template — `{0}` = user DN |
| `EmailAttribute` | `string` | `"mail"` | Attribute containing user email |
| `UsernameAttribute` | `string` | `"uid"` | Attribute containing username |
| `DisplayNameAttribute` | `string` | `"cn"` | Attribute containing display name |
| `GroupNameAttribute` | `string` | `"cn"` | Attribute containing group/role name |
| `ConnectionTimeoutSeconds` | `int` | `30` | TCP connection timeout |

The default section key is exposed as a constant:

```csharp
public const string SectionName = "Ldap"; // LdapOptions.SectionName
```

---

## Windows Active Directory

For Windows AD, override the default attribute names and filters:

```json
{
  "Ldap": {
    "Host": "dc.corp.example.com",
    "Port": 389,
    "BindDn": "CN=ServiceAccount,CN=Users,DC=corp,DC=example,DC=com",
    "BindPassword": "",
    "UserSearchBase": "CN=Users,DC=corp,DC=example,DC=com",
    "UserSearchFilter": "(&(objectClass=user)(sAMAccountName={0}))",
    "GroupSearchBase": "CN=Users,DC=corp,DC=example,DC=com",
    "GroupSearchFilter": "(&(objectClass=group)(member={0}))",
    "UsernameAttribute": "sAMAccountName",
    "DisplayNameAttribute": "displayName",
    "EmailAttribute": "mail",
    "GroupNameAttribute": "cn",
    "ConnectionTimeoutSeconds": 10
  }
}
```

---

## OpenTelemetry Instrumentation

Add the OpenTelemetry decorator after registering the core LDAP identity provider:

```csharp
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Bielu.AspNetCore.Identity.Ldap.OpenTelemetry.Extensions;

// 1. Register LDAP identity
builder.Services.AddLdapIdentity(builder.Configuration);

// 2. Wrap with OpenTelemetry instrumentation (decorator pattern via Scrutor)
builder.Services.AddLdapOpenTelemetryInstrumentation();

// 3. Wire up OTel exporters
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddLdapIdentityInstrumentation()     // subscribe to LDAP ActivitySource
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddLdapIdentityMetrics()             // subscribe to LDAP Meter
        .AddOtlpExporter());
```

### Exported Metrics

| Metric | Type | Unit | Description |
|---|---|---|---|
| `ldap.identity.authentication.attempts` | Counter | — | Total authentication attempts |
| `ldap.identity.authentication.successes` | Counter | — | Successful authentications |
| `ldap.identity.authentication.failures` | Counter | — | Failed authentications |
| `ldap.identity.authentication.duration` | Histogram | `s` | Authentication duration |
| `ldap.identity.search.operations` | Counter | — | Total search operations |
| `ldap.identity.search.duration` | Histogram | `s` | Search operation duration |
| `ldap.identity.operation.errors` | Counter | — | Total operation errors |

### Trace Spans

| Operation | Span Name |
|---|---|
| Credential validation | `ldap.validate_credentials` |
| Find user by username | `ldap.find_user` |
| Find user by DN | `ldap.find_user_by_dn` |
| Search users | `ldap.find_users` |
| List groups | `ldap.find_groups` |
| Get user groups | `ldap.get_user_groups` |

Span attributes include `ldap.username`, `ldap.user_dn`, and `ldap.result_count` where applicable.

---

## Testing

### Unit Tests

```bash
dotnet test test/Bielu.AspNetCore.Identity.Ldap.Tests
```

51 unit tests cover:
- `LdapOptions` — defaults, section name constant, property assignment
- `LdapEntry` — case-insensitive attribute lookup, null guards
- `LdapUserStore` / `LdapRoleStore` — find, create, update, delete contract
- `OpenTelemetryLdapServiceDecorator` — delegation, exception propagation, ActivitySource/Meter metadata
- `ServiceRegistrationTests` — both `IConfiguration` and fluent API overloads, custom section keys, null guards, OTel decorator wrapping

### Windows Integration Tests

Integration tests connect to a **real** Windows Active Directory server. They are **skipped automatically** unless explicitly enabled:

```bash
# Set the opt-in flag
export LDAP_INTEGRATION=true

# Override credentials via environment variables (LDAP_ prefix)
export LDAP_Host=dc.corp.example.com
export LDAP_BindDn="CN=ServiceAccount,CN=Users,DC=corp,DC=example,DC=com"
export LDAP_BindPassword="password"

dotnet test test/Bielu.AspNetCore.Identity.Ldap.Windows.Tests
```

Configuration is loaded from `appsettings.Integration.json` with `LDAP_` environment variable overrides.

---

## Project Structure

```
├── src/
│   ├── Bielu.AspNetCore.Identity.Ldap/
│   │   ├── Abstractions/          # ILdapService, LdapEntry
│   │   ├── Extensions/            # DI registration (IConfiguration + fluent API)
│   │   ├── Internal/              # LdapService implementation
│   │   ├── Stores/                # LdapUserStore, LdapRoleStore
│   │   ├── LdapOptions.cs         # Configuration with SectionName constant
│   │   ├── LdapUser.cs            # Identity user model
│   │   └── LdapRole.cs            # Identity role model
│   │
│   ├── Bielu.AspNetCore.Identity.Ldap.OpenTelemetry/
│   │   ├── Extensions/            # OTel DI registration
│   │   ├── Instrumentation/       # OpenTelemetryLdapServiceDecorator
│   │   ├── LdapActivitySource.cs  # ActivitySource + span names
│   │   └── LdapMetrics.cs         # Meter + counters/histograms
│   │
│   └── Bielu.AspNetCore.Identity.slnx
│
├── test/
│   ├── Bielu.AspNetCore.Identity.Ldap.Tests/           # Unit tests (xUnit + Shouldly + NSubstitute)
│   └── Bielu.AspNetCore.Identity.Ldap.Windows.Tests/   # Windows AD integration tests
│
├── Directory.Packages.props     # Central package management
├── version.props                # Version configuration
└── LICENSE                      # MIT License
```

---

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

Copyright © 2026 Arkadiusz Biel
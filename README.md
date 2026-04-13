# Bielu.AspNetCore.Identity.Ldap

[![NuGet](https://img.shields.io/nuget/v/Bielu.AspNetCore.Identity.Ldap.svg)](https://www.nuget.org/packages/Bielu.AspNetCore.Identity.Ldap/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Bielu.AspNetCore.Identity.Ldap.svg)](https://www.nuget.org/packages/Bielu.AspNetCore.Identity.Ldap/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Bielu.AspNetCore.Identity.Ldap is an ASP.NET Core Identity provider for **LDAP** directories. Authenticate users and resolve roles directly against any LDAP-compatible server (OpenLDAP, Active Directory, 389 Directory Server, etc.) without a local database.

Part of the [**bielu ecosystem**](https://github.com/bielu) — a collection of open-source .NET libraries for search, messaging, observability, and infrastructure.

> ⚠️ **Note:** Pre version 1.0.0, the API is regarded as unstable and **breaking changes may be introduced**.

## Key Features

- ✅ **Drop-in ASP.NET Core Identity stores** — `IUserStore<LdapUser>`, `IRoleStore<LdapRole>`, `IUserPasswordStore`, and `IUserEmailStore` implementations
- ✅ **Composable API** — `AddLdapStores` extends `IdentityBuilder` (like `AddEntityFrameworkStores`), or use `AddLdapServices` for mixed EF + LDAP scenarios
- ✅ **Cross-platform** — built on [`System.DirectoryServices.Protocols`](https://learn.microsoft.com/dotnet/api/system.directoryservices.protocols), no Windows-only COM or Novell dependencies
- ✅ **OpenTelemetry instrumentation** — optional package adds tracing spans and metrics counters/histograms to every LDAP operation via the [Scrutor](https://github.com/khellang/Scrutor) decorator pattern
- ✅ **IOptionsMonitor support** — hot-reload of LDAP settings when using configuration providers with change notifications
- ✅ **Overridable configuration section key** — defaults to `"Ldap"` (`LdapOptions.SectionName`), callers can pass any section name
- ✅ **Blazor compatible** — works with Blazor Server, Blazor WebAssembly (hosted), and Blazor Web App (interactive server)
- ✅ **Static analysis** — enforced via [Bielu.StaticCode.Analyzers](https://github.com/bielu/bielu.staticcode.analyzers), `Microsoft.CodeAnalysis.PublicApiAnalyzers`, and `Microsoft.VisualStudio.Threading.Analyzers`

## Installation

Install the packages from NuGet:

```bash
# Core package
dotnet add package Bielu.AspNetCore.Identity.Ldap

# (Optional) OpenTelemetry instrumentation
dotnet add package Bielu.AspNetCore.Identity.Ldap.OpenTelemetry
```

## Packages

| Package | Description |
|---------|-------------|
| `Bielu.AspNetCore.Identity.Ldap` | Core identity stores, `ILdapService`, models, and DI registration |
| `Bielu.AspNetCore.Identity.Ldap.OpenTelemetry` | OpenTelemetry tracing & metrics decorator for LDAP operations |

## Getting Started

### 1. Configure `appsettings.json`

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

### 2. Register Services

The library provides three levels of registration depending on your scenario:

#### Option A — LDAP-only (convenience shortcut)

When LDAP is the **only** identity store, use `AddLdapIdentity` which calls `AddIdentity<LdapUser, LdapRole>()` + `AddLdapStores()` in one call:

```csharp
using Bielu.AspNetCore.Identity.Ldap.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Calls AddIdentity + registers LdapOptions, ILdapService, and LDAP stores
builder.Services.AddLdapIdentity(builder.Configuration);
```

You can override the configuration section key:

```csharp
builder.Services.AddLdapIdentity(builder.Configuration, sectionName: "MyApp:Directory");
```

Or use the fluent API:

```csharp
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

#### Option B — Build on top of `AddIdentity` (composable)

When you want to control the Identity pipeline yourself — just like `AddEntityFrameworkStores<TContext>()` — use `AddLdapStores` as an extension on `IdentityBuilder`:

```csharp
using Bielu.AspNetCore.Identity.Ldap;
using Bielu.AspNetCore.Identity.Ldap.Extensions;

builder.Services
    .AddIdentity<LdapUser, LdapRole>(options =>
    {
        // Configure Identity options (lockout, password rules, etc.)
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddLdapStores(builder.Configuration);  // registers LdapOptions, ILdapService, and stores
```

This is the **recommended** pattern when you need to customize Identity options or chain additional Identity extensions.

#### Option C — Mixed stores (EF + LDAP)

When Entity Framework (or another provider) owns the Identity stores and you want to use LDAP only for **credential validation**, use `AddLdapServices` which registers `LdapOptions` and `ILdapService` **without** replacing the Identity stores:

```csharp
using Bielu.AspNetCore.Identity.Ldap.Abstractions;
using Bielu.AspNetCore.Identity.Ldap.Extensions;

// EF owns the Identity stores
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Register LDAP as a service (no stores — just ILdapService for credential validation)
builder.Services.AddLdapServices(builder.Configuration);
```

Then inject `ILdapService` to validate LDAP credentials against your existing EF users:

```csharp
public class HybridLoginService
{
    private readonly ILdapService _ldap;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public HybridLoginService(
        ILdapService ldap,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn)
    {
        _ldap = ldap;
        _users = users;
        _signIn = signIn;
    }

    public async Task<SignInResult> LoginAsync(string username, string password)
    {
        // Try LDAP first
        if (await _ldap.ValidateCredentialsAsync(username, password))
        {
            // Find or create a local EF user linked to the LDAP account
            var user = await _users.FindByNameAsync(username);
            if (user is null)
            {
                var ldapEntry = await _ldap.FindUserAsync(username);
                user = new ApplicationUser
                {
                    UserName = username,
                    Email = ldapEntry?.GetAttribute("mail"),
                    AuthenticationSource = "LDAP",
                };
                await _users.CreateAsync(user);
            }

            await _signIn.SignInAsync(user, isPersistent: false);
            return SignInResult.Success;
        }

        // Fall back to local password
        return await _signIn.PasswordSignInAsync(username, password, false, false);
    }
}
```

This pattern lets you:
- Use **EF stores** for local accounts, OpenID Connect external logins, and user management
- Use **LDAP** for credential validation against a corporate directory
- Gradually migrate users from LDAP to local accounts

## Examples

### Blazor Server / Blazor Web App (Interactive Server)

A complete Blazor Server example with LDAP authentication, `AuthenticationStateProvider`, and role-based `[Authorize]`:

**Program.cs:**

```csharp
using Bielu.AspNetCore.Identity.Ldap;
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Identity with LDAP stores (builds on top of AddIdentity)
builder.Services
    .AddIdentity<LdapUser, LdapRole>()
    .AddLdapStores(builder.Configuration);

// 2. Add authentication & authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddCookie(IdentityConstants.ApplicationScheme);

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// 3. Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

**Components/Pages/Login.razor:**

```razor
@page "/login"
@using Bielu.AspNetCore.Identity.Ldap
@using Microsoft.AspNetCore.Identity
@inject SignInManager<LdapUser> SignInManager
@inject NavigationManager Navigation

<PageTitle>Login</PageTitle>

<h3>LDAP Login</h3>

<EditForm Model="@loginModel" OnValidSubmit="HandleLogin" FormName="login">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label>Username</label>
        <InputText @bind-Value="loginModel.Username" class="form-control" />
    </div>
    <div class="mb-3">
        <label>Password</label>
        <InputText @bind-Value="loginModel.Password" type="password" class="form-control" />
    </div>

    @if (!string.IsNullOrEmpty(errorMessage))
    {
        <div class="alert alert-danger">@errorMessage</div>
    }

    <button type="submit" class="btn btn-primary">Sign In</button>
</EditForm>

@code {
    private LoginModel loginModel = new();
    private string? errorMessage;

    private async Task HandleLogin()
    {
        var result = await SignInManager.PasswordSignInAsync(
            loginModel.Username, loginModel.Password,
            isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            Navigation.NavigateTo("/", forceLoad: true);
        }
        else
        {
            errorMessage = "Invalid username or password.";
        }
    }

    private sealed class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
```

**Components/Pages/SecurePage.razor:**

```razor
@page "/secure"
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize]

<PageTitle>Secure Page</PageTitle>

<h3>Welcome, @context.User.Identity?.Name!</h3>
<p>You are authenticated via LDAP.</p>

<AuthorizeView Roles="Administrators">
    <Authorized>
        <p>🔑 You have the <strong>Administrators</strong> role.</p>
    </Authorized>
</AuthorizeView>

@code {
    [CascadingParameter]
    private Task<AuthenticationState> authStateTask { get; set; } = default!;

    private AuthenticationState context = default!;

    protected override async Task OnInitializedAsync()
    {
        context = await authStateTask;
    }
}
```

### Blazor WebAssembly (Hosted)

For Blazor WASM with a hosted ASP.NET Core backend, register LDAP identity on the **server** project and expose an authentication API:

**Server/Program.cs:**

```csharp
using Bielu.AspNetCore.Identity.Ldap;
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentity<LdapUser, LdapRole>()
    .AddLdapStores(builder.Configuration);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
})
.AddCookie(IdentityConstants.ApplicationScheme);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Minimal API login endpoint for the WASM client
app.MapPost("/api/login", async (
    LoginRequest request,
    SignInManager<LdapUser> signInManager) =>
{
    var result = await signInManager.PasswordSignInAsync(
        request.Username, request.Password,
        isPersistent: false, lockoutOnFailure: false);

    return result.Succeeded
        ? Results.Ok()
        : Results.Unauthorized();
});

app.MapPost("/api/logout", async (SignInManager<LdapUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});

app.MapGet("/api/me", (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        Name = context.User.Identity.Name,
        Roles = context.User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
    });
}).RequireAuthorization();

app.MapRazorPages();
app.MapFallbackToFile("index.html");

app.Run();

record LoginRequest(string Username, string Password);
```

### Minimal API

A simple Minimal API example for non-Blazor scenarios:

```csharp
using Bielu.AspNetCore.Identity.Ldap;
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentity<LdapUser, LdapRole>()
    .AddLdapStores(builder.Configuration);
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (LoginRequest req, SignInManager<LdapUser> signIn) =>
{
    var result = await signIn.PasswordSignInAsync(
        req.Username, req.Password, false, false);
    return result.Succeeded ? Results.Ok() : Results.Unauthorized();
});

app.MapGet("/me", (HttpContext ctx) => Results.Ok(new
{
    Name = ctx.User.Identity?.Name
})).RequireAuthorization();

app.Run();

record LoginRequest(string Username, string Password);
```

## Main Types

The main types provided by this library are:

| Type | Description |
|------|-------------|
| `LdapOptions` | Configuration options for LDAP connection, search bases, filters, and attribute mappings |
| `LdapUser` | ASP.NET Core Identity user backed by an LDAP entry (`IdentityUser` subclass) |
| `LdapRole` | ASP.NET Core Identity role backed by an LDAP group (`IdentityRole` subclass) |
| `LdapEntry` | Lightweight, read-only representation of an LDAP directory entry |
| `ILdapService` | High-level LDAP operations: credential validation, user/group search |
| `LdapUserStore` | `IUserStore<LdapUser>`, `IUserPasswordStore`, `IUserEmailStore` implementation |
| `LdapRoleStore` | `IRoleStore<LdapRole>` implementation |
| `OpenTelemetryLdapServiceDecorator` | `ILdapService` decorator adding tracing and metrics |
| `LdapActivitySource` | `ActivitySource` and span name constants for tracing |
| `LdapMetrics` | `Meter`, counters, and histograms for LDAP metrics |

## Configuration Reference

All properties live on `LdapOptions`:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
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

## OpenTelemetry Instrumentation

Add the OpenTelemetry decorator after registering the core LDAP identity provider:

```csharp
using Bielu.AspNetCore.Identity.Ldap;
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Bielu.AspNetCore.Identity.Ldap.OpenTelemetry.Extensions;

// 1. Register Identity with LDAP stores
builder.Services
    .AddIdentity<LdapUser, LdapRole>()
    .AddLdapStores(builder.Configuration);

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
|--------|------|------|-------------|
| `ldap.identity.authentication.attempts` | Counter | — | Total authentication attempts |
| `ldap.identity.authentication.successes` | Counter | — | Successful authentications |
| `ldap.identity.authentication.failures` | Counter | — | Failed authentications |
| `ldap.identity.authentication.duration` | Histogram | `s` | Authentication duration |
| `ldap.identity.search.operations` | Counter | — | Total search operations |
| `ldap.identity.search.duration` | Histogram | `s` | Search operation duration |
| `ldap.identity.operation.errors` | Counter | — | Total operation errors |

### Trace Spans

| Operation | Span Name |
|-----------|-----------|
| Credential validation | `ldap.validate_credentials` |
| Find user by username | `ldap.find_user` |
| Find user by DN | `ldap.find_user_by_dn` |
| Search users | `ldap.find_users` |
| List groups | `ldap.find_groups` |
| Get user groups | `ldap.get_user_groups` |

Span attributes include `ldap.username`, `ldap.user_dn`, and `ldap.result_count` where applicable.

## Testing

### Unit Tests

```bash
dotnet test test/Bielu.AspNetCore.Identity.Ldap.Tests
```

Unit tests cover:
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

## Contributing

We welcome contributions! Feel free to get involved by opening issues or submitting pull requests.

## Related Bielu Projects

| Project | Description |
|---------|-------------|
| [**Bielu.StaticCode.Analyzers**](https://github.com/bielu/bielu.staticcode.analyzers) | Static analysis rules used across bielu packages |
| [**Bielu.AspNetCore.AsyncApi**](https://github.com/bielu/Bielu.AspNetCore.AsyncApi) | Code-first AsyncAPI documentation generator for .NET |
| [**Bielu.PersistentQueues**](https://github.com/bielu/Bielu.PersistentQueues) | Fast persistent queues for .NET |
| [**Bielu.Common.Libraries**](https://github.com/bielu/Bielu.Common.Libraries) | Shared utilities and abstractions for the bielu ecosystem |

## Acknowledgments

This project builds upon:

- **[ASP.NET Core Identity](https://github.com/dotnet/aspnetcore)** — Microsoft's pluggable authentication and authorization framework
- **[System.DirectoryServices.Protocols](https://learn.microsoft.com/dotnet/api/system.directoryservices.protocols)** — Microsoft's cross-platform LDAP client
- **[Scrutor](https://github.com/khellang/Scrutor)** — Assembly scanning and decoration extensions for `IServiceCollection`
- **[OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)** — Observability framework for traces and metrics

## License

This project is licensed under the MIT License — see the [LICENSE](./LICENSE) file for details.
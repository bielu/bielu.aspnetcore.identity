# Blazor Hybrid Auth Example

This Blazor Web App demonstrates how to mix **three authentication sources** with a single ASP.NET Core Identity user store backed by Entity Framework Core (SQLite):

| Source | How it works |
|--------|-------------|
| **Local accounts** | Standard username/password registration stored in the EF database |
| **LDAP** | Credentials validated against an LDAP directory via `ILdapService`. A local EF user is auto-provisioned on first successful bind |
| **Microsoft Entra ID** | SSO via OpenID Connect. A local EF user is auto-provisioned on first `OnTokenValidated` callback |

## Architecture

```
┌────────────────────────────────────────────────────────┐
│                    Blazor Web App                      │
│                                                        │
│  Login Page ──► HybridSignInService                    │
│                   ├── 1. ILdapService.ValidateCredentials  │
│                   │      ↓ (if valid, provision EF user)   │
│                   └── 2. SignInManager.PasswordSignIn       │
│                          (local EF fallback)               │
│                                                        │
│  /challenge/EntraId ──► OIDC middleware                 │
│                          └── OnTokenValidated           │
│                               ↓ provision EF user       │
│                               ↓ SignInManager.SignIn    │
│                                                        │
│  Register Page ──► UserManager.CreateAsync (local)     │
│                                                        │
├────────────────────────────────────────────────────────┤
│  ASP.NET Core Identity                                 │
│  ├── AddIdentity<ApplicationUser, IdentityRole>()      │
│  ├── AddEntityFrameworkStores<ApplicationDbContext>()   │
│  └── AddDefaultTokenProviders()                        │
│                                                        │
│  LDAP (service-only, no stores)                        │
│  └── AddLdapServices(configuration)                    │
│       → registers ILdapService + LdapOptions           │
│                                                        │
│  Entra ID (external OIDC)                              │
│  └── AddOpenIdConnect("EntraId", ...)                  │
└────────────────────────────────────────────────────────┘
```

## Key Concept: `AddLdapServices` vs `AddLdapStores`

| Method | What it registers | When to use |
|--------|-------------------|-------------|
| `AddLdapStores(config)` | `ILdapService` + `LdapOptions` + `IUserStore<LdapUser>` + `IRoleStore<LdapRole>` | LDAP is the **only** Identity store |
| `AddLdapServices(config)` | `ILdapService` + `LdapOptions` only | Mixed scenario — EF owns the stores, LDAP is used for credential validation |

This example uses `AddLdapServices` because EF Core owns the Identity stores.

## Running the Example

### 1. Configure `appsettings.json`

Edit `appsettings.json` to point to your LDAP server and Entra ID tenant:

```json
{
  "Ldap": {
    "Host": "your-ldap-server",
    "Port": 389,
    "BindDn": "cn=readonly,dc=example,dc=com",
    "BindPassword": "your-password",
    "UserSearchBase": "ou=People,dc=example,dc=com"
  },
  "EntraId": {
    "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

### 2. Run

```bash
cd examples/Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth
dotnet run
```

The app creates a SQLite database (`app.db`) automatically on first run.

### 3. Test each auth path

- **Local account**: Navigate to `/register`, create an account, then sign in at `/login`
- **LDAP**: Enter LDAP credentials on the `/login` page — the `HybridSignInService` tries LDAP first
- **Entra ID**: Click "Sign in with Entra ID" on the `/login` page (requires valid Entra ID config)

All three paths create/reuse a single `ApplicationUser` record in the EF database.

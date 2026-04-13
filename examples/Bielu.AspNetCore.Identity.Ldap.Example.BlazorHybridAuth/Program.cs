using Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth;
using Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Components;
using Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Data;
using Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Services;
using Bielu.AspNetCore.Identity.Ldap.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────
//  1. EF Core + ASP.NET Core Identity  (local accounts — the primary store)
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Relax password rules for the demo — tighten in production!
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;

        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ─────────────────────────────────────────────────────────────────────────
//  2. LDAP  (service-only — ILdapService for credential validation,
//           EF still owns the Identity stores)
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddLdapServices(builder.Configuration);

// ─────────────────────────────────────────────────────────────────────────
//  3. Microsoft Entra ID  (via OpenID Connect)
// ─────────────────────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication()
    .AddOpenIdConnect("EntraId", "Microsoft Entra ID", options =>
    {
        var entraSection = builder.Configuration.GetSection("EntraId");

        options.Authority = entraSection["Authority"];
        options.ClientId = entraSection["ClientId"];
        options.ClientSecret = entraSection["ClientSecret"];

        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;

        options.CallbackPath = entraSection["CallbackPath"] ?? "/signin-oidc";

        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.GetClaimsFromUserInfoEndpoint = true;

        options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
        {
            OnTokenValidated = async ctx =>
            {
                var userManager = ctx.HttpContext.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();

                var signInManager = ctx.HttpContext.RequestServices
                    .GetRequiredService<SignInManager<ApplicationUser>>();

                var email = ctx.Principal?.FindFirst("email")?.Value
                         ?? ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                if (email is null)
                {
                    ctx.Fail("Email claim not found in the token.");
                    return;
                }

                var user = await userManager.FindByEmailAsync(email);

                // Provision a local account linked to the Entra ID identity.
                if (user is null)
                {
                    var name = ctx.Principal?.FindFirst("name")?.Value
                            ?? ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? email;

                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        DisplayName = name,
                        AuthenticationSource = "EntraId",
                    };

                    await userManager.CreateAsync(user);
                }

                // Sign in with Identity so the cookie is issued under the
                // Identity.Application scheme used by the rest of the app.
                await signInManager.SignInAsync(user, isPersistent: false);
            },
        };
    });

// ─────────────────────────────────────────────────────────────────────────
//  4. Application services
// ─────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<HybridSignInService>();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────
//  Middleware pipeline
// ─────────────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// ─────────────────────────────────────────────────────────────────────────
//  Ensure the SQLite database exists and is migrated
// ─────────────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.MapAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

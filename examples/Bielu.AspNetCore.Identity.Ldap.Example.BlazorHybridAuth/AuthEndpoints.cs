using Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Data;
using Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth;

/// <summary>
/// Registers minimal API endpoints for authentication operations that require
/// HTTP-level redirects (challenge, sign-out) which cannot be done inside
/// interactive Blazor Server components.
/// </summary>
public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        // ── External challenge (Entra ID / any OIDC scheme) ──────────
        app.MapGet("/challenge/{scheme}", async (string scheme, HttpContext ctx) =>
        {
            var properties = new AuthenticationProperties { RedirectUri = "/secure" };
            await ctx.ChallengeAsync(scheme, properties);
        }).AllowAnonymous();

        // ── Local / LDAP password sign-in ────────────────────────────
        // Blazor interactive server components cannot set auth cookies from
        // @onclick handlers. This POST endpoint handles the form submission.
        app.MapPost("/login", async (HttpContext ctx) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ctx.Response.Redirect("/login?error=Username+and+password+are+required");
                return;
            }

            var hybridSignIn = ctx.RequestServices.GetRequiredService<HybridSignInService>();
            var result = await hybridSignIn.PasswordSignInAsync(username, password);

            if (result.Succeeded)
            {
                ctx.Response.Redirect("/secure");
            }
            else if (result.IsLockedOut)
            {
                ctx.Response.Redirect("/login?error=Account+is+locked+out");
            }
            else
            {
                ctx.Response.Redirect("/login?error=Invalid+username+or+password");
            }
        }).AllowAnonymous();

        // ── Sign-out (POST to prevent CSRF via link) ─────────────────
        app.MapPost("/logout", async (HttpContext ctx) =>
        {
            var signInManager = ctx.RequestServices
                .GetRequiredService<SignInManager<ApplicationUser>>();

            await signInManager.SignOutAsync();
            ctx.Response.Redirect("/");
        }).AllowAnonymous();

        return app;
    }
}

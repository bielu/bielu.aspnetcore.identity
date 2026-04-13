using Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Data;
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

        // ── Sign-out ─────────────────────────────────────────────────
        app.MapGet("/logout", async (HttpContext ctx) =>
        {
            var signInManager = ctx.RequestServices
                .GetRequiredService<SignInManager<ApplicationUser>>();

            await signInManager.SignOutAsync();
            ctx.Response.Redirect("/");
        }).AllowAnonymous();

        return app;
    }
}

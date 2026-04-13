using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bielu.AspNetCore.Identity.Ldap.Example.BlazorHybridAuth.Data;

/// <summary>
/// EF Core database context that stores ASP.NET Core Identity tables
/// (users, roles, claims, tokens, etc.) in a local SQLite database.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}

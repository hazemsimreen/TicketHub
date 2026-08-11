using Contracts.Security;
using DataAccess.Context;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Auth;

/// <summary>
/// Seeds the 4 system roles into the database on startup.
/// Called automatically when the application starts.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        var roleNames = new[] { AppRoles.Admin, AppRoles.Supervisor, AppRoles.Agent, AppRoles.Citizen };
        foreach (var name in roleNames)
        {
            if (!await db.Set<Role>().AnyAsync(r => r.Code == name))
            {
                db.Set<Role>().Add(new Role { Code = name });
            }
        }

        await db.SaveChangesAsync();
    }
}

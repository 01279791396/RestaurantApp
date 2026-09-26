using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantApp.Models;

namespace RestaurantApp.Data
{
    // Seeds fixed data that the app needs to run: the three roles, and one default admin
    // account so there's always a way into the admin dashboard on a fresh database.
    public static class DbInitializer
    {
        public static readonly string[] Roles = { "Admin", "Customer" };

        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = configuration["SeedAdmin:Email"];
            var adminPassword = configuration["SeedAdmin:Password"];
            var adminFullName = configuration["SeedAdmin:FullName"] ?? "Admin";

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                // Missing config is a setup mistake worth failing loudly on, rather than
                // silently running with no admin account at all.
                throw new InvalidOperationException(
                    "SeedAdmin:Email and SeedAdmin:Password must be set in configuration.");
            }

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = adminFullName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
                else
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to seed admin user: {errors}");
                }
            }

            var db = services.GetRequiredService<ApplicationDbContext>();
            if (!await db.RestaurantSettings.AnyAsync())
            {
                db.RestaurantSettings.Add(new RestaurantSettings());
                await db.SaveChangesAsync();
            }
        }
    }
}

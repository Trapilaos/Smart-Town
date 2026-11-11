using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

namespace API.Data
{
    public static class UserSeed
    {
        /// <summary>Public password of the demo residents. Only seeded when Seed:DemoData is true.</summary>
        public const string DemoPassword = "Pa$$w0rd";

        /// <summary>The Development-only admin password. Other environments take Seed:AdminPassword instead.</summary>
        public const string DevelopmentAdminPassword = "Pa$$w0rd";

        public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager,
            string contentRootPath, SeedOptions options, ILogger logger)
        {
            await EnsureRolesAsync(roleManager);

            var anyUsers = await userManager.Users.AnyAsync();

            if (options.DemoData && !anyUsers)
            {
                await SeedDemoResidentsAsync(userManager, contentRootPath);
            }

            if (options.IsDevelopment)
            {
                // Development keeps its original behaviour: the known admin is created with the first seed only.
                if (!anyUsers)
                {
                    await CreateAdminAsync(userManager, options.AdminUsername, DevelopmentAdminPassword, logger);
                }
                return;
            }

            // Other environments: the admin comes only from Seed:AdminPassword, and only when no admin exists yet.
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count > 0) return;

            if (string.IsNullOrEmpty(options.AdminPassword))
            {
                logger.LogWarning("No admin account exists. Set Seed__AdminPassword to create the first admin " +
                    "(at least 6 characters, with an upper case letter, a lower case letter and a digit).");
                return;
            }

            await CreateAdminAsync(userManager, options.AdminUsername, options.AdminPassword, logger);
        }

        private static async Task EnsureRolesAsync(RoleManager<AppRole> roleManager)
        {
            foreach (var name in new[] { "Member", "Admin", "Moderator" })
            {
                if (!await roleManager.RoleExistsAsync(name))
                {
                    await roleManager.CreateAsync(new AppRole { Name = name });
                }
            }
        }

        private static async Task SeedDemoResidentsAsync(UserManager<AppUser> userManager, string contentRootPath)
        {
            var userData = await File.ReadAllTextAsync(Path.Combine(contentRootPath, "Data", "UserSeedData.json"));
            var users = JsonSerializer.Deserialize<List<AppUser>>(userData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (users == null) return;

            foreach (var user in users)
            {
                user.UserName = user.UserName.ToLower();
                await userManager.CreateAsync(user, DemoPassword);
                await userManager.AddToRoleAsync(user, "Member");
            }
        }

        /// <summary>Creates the admin with the Admin and Moderator roles. Never logs the password.</summary>
        private static async Task CreateAdminAsync(UserManager<AppUser> userManager, string username, string password, ILogger logger)
        {
            var admin = new AppUser { UserName = username };

            var created = await userManager.CreateAsync(admin, password);
            if (!created.Succeeded)
            {
                // The errors name the rule that failed (for example, too short), never the password itself.
                logger.LogWarning("Admin account '{Username}' was not created: {Errors}. " +
                    "Set Seed__AdminPassword to create the first admin.",
                    username, string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" });
            logger.LogInformation("Created the admin account '{Username}'", username);
        }
    }
}

using System.Text.Json;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace API.Data
{
    public static class Seed
    {
        public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, ILogger<Program> logger)
        {
            if (await userManager.Users.AnyAsync())
            {
                logger.LogInformation("Users already exist in the database. Skipping seeding.");
                return;
            }

            logger.LogInformation("Seeding users...");

            var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

            if (users == null)
            {
                logger.LogError("Failed to deserialize user data.");
                return;
            }

            var roles = new List<AppRole>
            {
                new AppRole { Name = "Member" },
                new AppRole { Name = "Admin" },
                new AppRole { Name = "Moderator" }
            };

            foreach (var role in roles)
            {
                await roleManager.CreateAsync(role);
            }

            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];
                user.UserName = user.UserName.ToLower();
                var result = await userManager.CreateAsync(user, "Pa$$w0rd");

                if (result.Succeeded)
                {
                    logger.LogInformation($"Successfully created user: {user.UserName}");

                    if (i == users.Count - 2) // Second-to-last user gets "Admin"
                    {
                        await userManager.AddToRoleAsync(user, "Admin");
                    }
                    else if (i == users.Count - 1) // Last user gets "Moderator"
                    {
                        await userManager.AddToRoleAsync(user, "Moderator");
                    }
                    else
                    {
                        await userManager.AddToRoleAsync(user, "Member");
                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        logger.LogError($"Error creating user {user.UserName}: {error.Description}");
                    }
                }
            }
        }
    }
}

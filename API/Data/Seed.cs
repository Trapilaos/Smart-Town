using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, DataContext context)
        {
            if (await userManager.Users.AnyAsync()) return;

            var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");
            var users = JsonSerializer.Deserialize<List<AppUser>>(userData);
            if (users == null) return;

            var roles = new List<AppRole>
            {
                new AppRole{Name = "Member"},
                new AppRole{Name = "Admin"},
                new AppRole{Name = "Moderator"},
            };

            foreach (var role in roles)
            {
                await roleManager.CreateAsync(role);
            }

            foreach (var user in users)
            {
                user.UserName = user.UserName.ToLower();
                await userManager.CreateAsync(user, "Pa$$w0rd");
                await userManager.AddToRoleAsync(user, "Member");
            }

            var admin = new AppUser
            {
                UserName = "admin"
            };

            await userManager.CreateAsync(admin, "Pa$$w0rd");
            await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" });

            await SeedParkingSpaces(context);
            await SeedTrafficData(context);
        }

        public static async Task SeedParkingSpaces(DataContext context)
        {
            if (!await context.ParkingSpaces.AnyAsync())
            {
                var parkingData = await File.ReadAllTextAsync("Data/parking-spaces.json");
                var parkingSpaces = JsonSerializer.Deserialize<List<ParkingSpace>>(parkingData);

                if (parkingSpaces != null)
                {
                    await context.ParkingSpaces.AddRangeAsync(parkingSpaces);
                    await context.SaveChangesAsync();
                }
            }
        }

        public static async Task SeedTrafficData(DataContext context)
        {
            if (!await context.TrafficData.AnyAsync())
            {
                var trafficData = TrafficDataGenerator.GenerateTrafficData();
                await context.TrafficData.AddRangeAsync(trafficData);
                await context.SaveChangesAsync();
            }
        }
    }
}

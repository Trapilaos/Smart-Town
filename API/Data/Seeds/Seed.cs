using API.Data.Seeds;
using API.Entities;
using Microsoft.AspNetCore.Identity;

namespace API.Data
{
    /// <summary>Which seed steps run in this environment. Read from the Seed section of configuration.</summary>
    public sealed record SeedOptions(bool DemoData, bool IsDevelopment, string AdminUsername, string AdminPassword)
    {
        public const string DefaultAdminUsername = "admin";

        public static SeedOptions FromConfiguration(IConfiguration config, IHostEnvironment env)
        {
            var username = config["Seed:AdminUsername"];
            return new SeedOptions(
                DemoData: config.GetValue("Seed:DemoData", env.IsDevelopment()),
                IsDevelopment: env.IsDevelopment(),
                AdminUsername: string.IsNullOrWhiteSpace(username) ? DefaultAdminUsername : username.Trim().ToLowerInvariant(),
                AdminPassword: config["Seed:AdminPassword"]);
        }
    }

    public static class Seed
    {
        public static async Task SeedData(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, DataContext context,
            SeedOptions options, string contentRootPath, ILogger logger)
        {
            await UserSeed.SeedUsers(userManager, roleManager, contentRootPath, options, logger);
            await ParkingSpaceSeed.SeedParkingSpaces(context, contentRootPath);
            await TrafficDataSeed.SeedTrafficData(context);
            await InvoiceSeed.SeedInvoices(context, options.DemoData);
            await EventSeed.SeedEventsAsync(context);
            await MunicipalitySeed.SeedMunicipalityAsync(context);
            await WasteBinSeed.SeedWasteBinsAsync(context);
        }
    }
}

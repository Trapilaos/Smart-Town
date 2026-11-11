using API.Data.Seeds;
using API.Entities;
using Microsoft.AspNetCore.Identity;

namespace API.Data
{
    public static class Seed
    {
        public static async Task SeedData(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, DataContext context, string contentRootPath)
        {
            await UserSeed.SeedUsers(userManager, roleManager, contentRootPath);
            await ParkingSpaceSeed.SeedParkingSpaces(context, contentRootPath);
            await TrafficDataSeed.SeedTrafficData(context);
            await InvoiceSeed.SeedInvoices(context);
            await WasteBinSeed.SeedWasteBinsAsync(context);
        }
    }
}

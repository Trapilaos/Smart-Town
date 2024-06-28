using API.Data.Seeds;
using API.Entities;
using Microsoft.AspNetCore.Identity;

namespace API.Data
{
    public static class Seed
    {
        public static async Task SeedData(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, DataContext context)
        {
            await UserSeed.SeedUsers(userManager, roleManager);
            await ParkingSpaceSeed.SeedParkingSpaces(context);
            await TrafficDataSeed.SeedTrafficData(context);
            await InvoiceSeed.SeedInvoices(context);
            await WasteBinSeed.SeedWasteBinsAsync(context);
        }
    }
}

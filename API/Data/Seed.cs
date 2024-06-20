using System.Text.Json;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedUsers(UserManager<AppUser> userManager, 
            RoleManager<AppRole> roleManager, DataContext context)
        {
            if (await userManager.Users.AnyAsync()) return;

            var userData = await System.IO.File.ReadAllTextAsync("Data/UserSeedData.json");
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
            await userManager.AddToRolesAsync(admin, new[] {"Admin", "Moderator"});

            await SeedInvoices(context);
        }

        private static async Task SeedInvoices(DataContext context)
        {
            if (await context.Invoices.AnyAsync()) return;

            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceNumber = "INV-001", Amount = 100.00M, UserId = "1", IsPaid = false },
                new Invoice { InvoiceNumber = "INV-002", Amount = 150.50M, UserId = "2", IsPaid = false },
                new Invoice { InvoiceNumber = "INV-003", Amount = 200.75M, UserId = "3", IsPaid = false },
                new Invoice { InvoiceNumber = "INV-004", Amount = 120.00M, UserId = "4", IsPaid = false },
                new Invoice { InvoiceNumber = "INV-005", Amount = 300.20M, UserId = "5", IsPaid = false },
                new Invoice { InvoiceNumber = "INV-006", Amount = 80.00M, UserId = "6", IsPaid = false }
            };

            await context.Invoices.AddRangeAsync(invoices);
            await context.SaveChangesAsync();
        }
    }
}

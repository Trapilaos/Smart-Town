using System.Net;
using System.Net.Http.Json;
using API.Data;
using API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests;

/// <summary>Production host with no Seed:AdminPassword: only municipal content is seeded.</summary>
public sealed class ProductionApiFactory : ApiFactory
{
    protected override string EnvironmentName => "Production";
    protected override IReadOnlyDictionary<string, string> SeedSettings => new Dictionary<string, string>();
}

/// <summary>Production host that creates the first admin from Seed:AdminPassword.</summary>
public sealed class ProductionWithAdminFactory : ApiFactory
{
    public const string ProductionAdminPassword = "Adm1nPassw0rd";

    protected override string EnvironmentName => "Production";
    protected override IReadOnlyDictionary<string, string> SeedSettings => new Dictionary<string, string>
    {
        ["Seed:AdminPassword"] = ProductionAdminPassword,
    };
}

[Collection(ApiCollection.Name)]
public class SeedingTests
{
    [Fact]
    public async Task Production_creates_no_demo_users_and_no_admin_without_a_seed_password()
    {
        using var factory = new ProductionApiFactory();
        var client = factory.CreateApiClient();

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        Assert.Equal(0, await context.Users.CountAsync());
        Assert.NotEmpty(await context.ParkingSpaces.ToListAsync()); // municipal content is still seeded

        var demoResident = await client.PostAsJsonAsync("/api/account/login", new { username = "lisa", password = ApiFactory.AdminPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, demoResident.StatusCode);

        var publicAdmin = await client.PostAsJsonAsync("/api/account/login", new { username = "admin", password = ApiFactory.AdminPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, publicAdmin.StatusCode);
    }

    [Fact]
    public async Task Production_with_a_seed_password_creates_only_that_admin()
    {
        using var factory = new ProductionWithAdminFactory();
        var client = factory.CreateApiClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();

            Assert.Equal(1, await context.Users.CountAsync());
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            Assert.Equal("admin", Assert.Single(admins).UserName);
        }

        var login = await client.PostAsJsonAsync("/api/account/login",
            new { username = "admin", password = ProductionWithAdminFactory.ProductionAdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var publicPassword = await client.PostAsJsonAsync("/api/account/login", new { username = "admin", password = ApiFactory.AdminPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, publicPassword.StatusCode);
    }
}

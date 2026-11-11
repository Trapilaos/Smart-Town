using API.Data;
using API.Entities;
using API.Extensions;
using API.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                // Test/diagnostic endpoints must never be reachable outside Development.
                if (!builder.Environment.IsDevelopment())
                    manager.FeatureProviders.Add(new DevelopmentOnlyControllerFeatureProvider());
            });
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddIdentityServices(builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseMiddleware<ExceptionMiddleware>();

        var allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "https://localhost:4200" };
        app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().WithOrigins(allowedOrigins));

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<DataContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
            await context.Database.MigrateAsync();
            await Seed.SeedData(userManager, roleManager, context, app.Environment.ContentRootPath);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred during migration");
        }

        app.Run();
    }
}

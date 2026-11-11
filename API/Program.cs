using API.Data;
using API.Entities;
using API.Extensions;
using API.Helpers;
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
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));
        builder.Services.AddOpenApi();
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddIdentityServices(builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        // Security headers go first so every response carries them, including error responses.
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<ExceptionMiddleware>();

        var allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "https://localhost:4200" };
        app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().WithOrigins(allowedOrigins));

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        if (app.Environment.IsDevelopment())
        {
            // OpenAPI document at /openapi/v1.json (Development only)
            app.MapOpenApi();
        }

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        try
        {
            var context = services.GetRequiredService<DataContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
            await context.Database.MigrateAsync();
            var seedOptions = SeedOptions.FromConfiguration(app.Configuration, app.Environment);
            await Seed.SeedData(userManager, roleManager, context, seedOptions, app.Environment.ContentRootPath, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during migration or seeding");
        }

        app.Run();
    }
}

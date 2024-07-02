using API.Data;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddIdentityServices(builder.Configuration);

        // Register HttpClient
        builder.Services.AddHttpClient<SmartLightingService>();

        // Register TrafficService and ParkingService
        builder.Services.AddScoped<ITrafficService, TrafficService>();
        builder.Services.AddScoped<IParkingService, ParkingService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IWasteManagementService, WasteManagementService>();

        // Register EventService and CommentService
        builder.Services.AddScoped<IEventService, EventService>();
        builder.Services.AddScoped<ICommentService, CommentService>();

        // Register TrafficDataBackgroundService
        builder.Services.AddHostedService<TrafficDataBackgroundService>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
            options.AddPolicy("RequireMemberRole", policy => policy.RequireRole("Member"));
        });
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseMiddleware<ExceptionMiddleware>();

        app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().WithOrigins("https://localhost:4200"));

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
            var logger = services.GetRequiredService<ILogger<Program>>();
            await context.Database.MigrateAsync();
            await Seed.SeedData(userManager, roleManager, context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred during migration");
        }

        app.Run();
    }
}

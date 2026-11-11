using API.Data;
using API.Interfaces;
using API.Helpers;
using API.Services;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<DataContext>(opt =>
            {
                opt.UseSqlite(config.GetConnectionString("DefaultConnection"));
            });
            services.AddCors();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITrafficService, TrafficService>();
            services.AddScoped<IParkingService, ParkingService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IWasteService, WasteService>();
            services.AddScoped<IMunicipalityService, MunicipalityService>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddHttpClient<BoundaryLookupService>();
            services.AddForwardedHeaderTrust(config);
            services.AddMemoryCache();
            services.Configure<LoginThrottleOptions>(config.GetSection("LoginThrottle"));
            services.AddSingleton<LoginThrottle>();
            services.AddRateLimitPolicies(config);
            services.AddHostedService<BoundaryAutoFetchService>();
            services.AddHostedService<TrafficDataBackgroundService>();
            services.AddHostedService<ReservationCleanupService>();
            services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);

            return services;
        }
    }
}

using API.Data;
using API.Interfaces;
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
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IWasteManagementService, WasteManagementService>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddHttpClient<SmartLightingService>();
            services.AddHostedService<TrafficDataBackgroundService>();
            services.AddAutoMapper(typeof(MappingProfiles).Assembly);

            return services;
        }
    }
}

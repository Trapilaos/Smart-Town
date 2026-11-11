using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Extensions
{
    public static class RateLimitPolicies
    {
        public const string BoundarySearch = "boundary-search";

        /// <summary>Registers the named rate-limit policies. Limits come from configuration so tests can raise them.</summary>
        public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services, IConfiguration config)
        {
            var boundarySearchPerMinute = config.GetValue("RateLimits:BoundarySearchPerMinute", 10);

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = (context, _) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                    }
                    return ValueTask.CompletedTask;
                };

                options.AddPolicy(BoundarySearch, context => RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = boundarySearchPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
            });

            return services;
        }

        private static string ClientKey(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

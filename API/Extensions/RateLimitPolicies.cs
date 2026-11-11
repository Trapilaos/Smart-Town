using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Extensions
{
    /// <summary>
    /// The single rate limiter registration. Partitioned by client IP. Limits come from the RateLimits section,
    /// so tests can raise them. Rejected requests get 429 with Retry-After.
    /// </summary>
    public static class RateLimitPolicies
    {
        public const string BoundarySearch = "boundary-search";
        public const string Auth = "auth";
        public const string PublicInvoice = "public-invoice";

        public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services, IConfiguration config)
        {
            var boundarySearchPerMinute = config.GetValue("RateLimits:BoundarySearchPerMinute", 10);
            var authPerMinute = config.GetValue("RateLimits:AuthPerMinute", 10);
            var publicInvoicePerMinute = config.GetValue("RateLimits:PublicInvoicePerMinute", 5);
            var globalPerMinute = config.GetValue("RateLimits:GlobalPerMinute", 300);

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

                // Applies to every request. Named policies below are added on top where an endpoint asks for them.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => Window(globalPerMinute)));

                options.AddPolicy(BoundarySearch, context => RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context), _ => Window(boundarySearchPerMinute)));
                options.AddPolicy(Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context), _ => Window(authPerMinute)));
                options.AddPolicy(PublicInvoice, context => RateLimitPartition.GetFixedWindowLimiter(
                    ClientKey(context), _ => Window(publicInvoicePerMinute)));
            });

            return services;
        }

        private static FixedWindowRateLimiterOptions Window(int permitsPerMinute) => new()
        {
            PermitLimit = permitsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        };

        private static string ClientKey(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace API.Extensions
{
    /// <summary>
    /// Trusts X-Forwarded-For only when the request comes from a configured proxy. Behind nginx the TCP peer is
    /// the proxy, so without this every citizen would share the proxy's rate-limit bucket.
    /// Unknown peers' forwarded headers are ignored, so a client cannot choose its own address.
    /// Loopback stays trusted by default. Configuration (ForwardedHeaders:KnownNetworks as CIDR strings and
    /// ForwardedHeaders:KnownProxies as IP addresses) replaces that default when either list is set.
    /// </summary>
    public static class ForwardedHeadersSettings
    {
        public static IServiceCollection AddForwardedHeaderTrust(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // Only the entry our own proxy appended is used. Anything the client put before it is ignored.
                options.ForwardLimit = 1;

                var networks = config.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
                var proxies = config.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];

                if (networks.Length > 0 || proxies.Length > 0)
                {
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                }

                foreach (var cidr in networks)
                    options.KnownNetworks.Add(Microsoft.AspNetCore.HttpOverrides.IPNetwork.Parse(cidr));

                foreach (var ip in proxies)
                    options.KnownProxies.Add(IPAddress.Parse(ip));
            });

            return services;
        }
    }
}

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace API.Tests;

/// <summary>
/// Test-only. TestServer has no TCP connection, so the peer address is null. This filter sets it from the
/// X-Test-Remote-Ip header before any production middleware runs. It is registered only by ApiFactory.
/// </summary>
public sealed class TestRemoteIpFilter : IStartupFilter
{
    public const string Header = "X-Test-Remote-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Headers.TryGetValue(Header, out var value) && IPAddress.TryParse(value.ToString(), out var ip))
            {
                context.Connection.RemoteIpAddress = ip;
            }
            await nextMiddleware();
        });
        next(app);
    };
}

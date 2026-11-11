using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Tests;

/// <summary>
/// Boots the real API against a named, shared-cache in-memory SQLite database.
/// A keeper connection stays open for the factory's lifetime so the database survives.
/// Configuration is passed through environment variables because Program reads it while building.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminPassword = "Pa$$w0rd"; // seeded by UserSeed

    private readonly SqliteConnection _keeper;
    private readonly Dictionary<string, string> _previousEnv = new();

    public ApiFactory()
    {
        var connectionString = $"Data Source=file:smarttown-{Guid.NewGuid():N}?mode=memory&cache=shared";
        _keeper = new SqliteConnection(connectionString);
        _keeper.Open();

        SetEnv("ConnectionStrings__DefaultConnection", connectionString);
        SetEnv("TokenKey", new string('k', 96) + "-test-only-signing-key");
        // Tests never reach OpenStreetMap: no automatic boundary fetch, and Nominatim calls hit the stub below.
        SetEnv("Municipality__AutoFetchBoundary", "false");
        SetEnv("Municipality__NominatimMinIntervalMs", "0");
    }

    /// <summary>Nominatim answers with no candidates, so admin boundary search and apply stay offline.</summary>
    public StubHttpHandler NominatimStub { get; } = new(_ => StubHttpHandler.Json("[]"));

    /// <summary>
    /// Rate limits for this host. The default values are high enough that the suite never trips them, except
    /// BoundarySearchPerMinute = 10, which the boundary search test relies on. Subclasses override single values.
    /// </summary>
    protected virtual IReadOnlyDictionary<string, string> RateLimitSettings => new Dictionary<string, string>
    {
        ["RateLimits:BoundarySearchPerMinute"] = "10",
        ["RateLimits:AuthPerMinute"] = "100000",
        ["RateLimits:PublicInvoicePerMinute"] = "100000",
        ["RateLimits:GlobalPerMinute"] = "100000",
    };

    private void SetEnv(string name, string value)
    {
        _previousEnv[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Warning));
        foreach (var (key, value) in RateLimitSettings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services =>
        {
            // The traffic generator wipes and refills data from an async void timer; keep it out of tests.
            var hosted = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var d in hosted) services.Remove(d);

            services.AddHttpClient<API.Services.BoundaryLookupService>()
                .ConfigurePrimaryHttpMessageHandler(() => NominatimStub);
        });
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _keeper.Dispose();
        foreach (var (name, value) in _previousEnv)
            Environment.SetEnvironmentVariable(name, value);
    }
}

/// <summary>One host and one database for all API tests; config travels via process-wide env vars so factories must not run in parallel.</summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}

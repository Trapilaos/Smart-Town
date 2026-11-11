using System.Text.Json;
using API.Data;
using API.Data.Seeds;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Geo;
using API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Tests;

/// <summary>Seed data, OpenStreetMap lookup and boundary handling. Never touches the real network.</summary>
public class MunicipalityTests : IDisposable
{
    private const string SearchFixture = """
        [
          {"place_id":1,"osm_type":"relation","osm_id":1234,"lat":"37.8650","lon":"23.7530",
           "category":"boundary","type":"administrative","name":"Glyfada","display_name":"Glyfada, Attica, Greece",
           "geojson":{"type":"Polygon","coordinates":[[[23.73,37.87],[23.79,37.87],[23.79,37.84],[23.73,37.84],[23.73,37.87]]]}},
          {"place_id":2,"osm_type":"node","osm_id":77,"lat":"37.1","lon":"23.1",
           "category":"place","type":"town","name":"Glyfada","display_name":"Glyfada town",
           "geojson":{"type":"Point","coordinates":[23.1,37.1]}},
          {"place_id":3,"osm_type":"relation","osm_id":99,"lat":"1","lon":"1",
           "category":"boundary","type":"administrative","name":"Broken","display_name":"Broken",
           "geojson":{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1]]]}},
          {"place_id":4,"osm_type":"way","osm_id":5,"lat":"37.2","lon":"23.2",
           "class":"boundary","type":"administrative","name":"Other","display_name":"Other, Greece",
           "geojson":{"type":"Polygon","coordinates":[[[23,37],[23.1,37],[23.1,37.1],[23,37]]]}}
        ]
        """;

    private const string LookupFixture = """
        [{"place_id":1,"osm_type":"relation","osm_id":1234,"lat":"37.8650","lon":"23.7530",
          "category":"boundary","type":"administrative","name":"Glyfada","display_name":"Glyfada, Attica, Greece",
          "geojson":{"type":"Polygon","coordinates":[[[23.73,37.87],[23.79,37.87],[23.79,37.84],[23.73,37.84],[23.73,37.87]]]}}]
        """;

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _provider;

    public MunicipalityTests()
    {
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<DataContext>(o => o.UseSqlite(_connection));
        services.AddScoped<IMunicipalityService, MunicipalityService>();
        services.AddLogging();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        context.Database.EnsureCreated();
        MunicipalitySeed.SeedMunicipalityAsync(context).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    private static IConfiguration Config(bool autoFetch = true, int intervalMs = 0) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Municipality:AutoFetchBoundary"] = autoFetch ? "true" : "false",
            ["Municipality:NominatimMinIntervalMs"] = intervalMs.ToString(),
        })
        .Build();

    private static BoundaryLookupService CreateLookup(StubHttpHandler handler)
    {
        var client = new HttpClient(handler);
        return new BoundaryLookupService(client, Config(), NullLogger<BoundaryLookupService>.Instance);
    }

    private static StubHttpHandler Responding(string body, System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK) =>
        new(_ => StubHttpHandler.Json(body, status));

    private async Task<Municipality> LoadMunicipalityAsync()
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DataContext>().Municipalities.AsNoTracking().SingleAsync();
    }

    [Fact]
    public void Seeded_polygon_is_an_approximate_glyfada_outline_with_12_to_20_vertices()
    {
        var geometry = GeoService.ParseGeoJson(MunicipalitySeed.ApproximateBoundaryGeoJson);

        Assert.Single(geometry.Polygons);
        var ring = geometry.Polygons[0].Rings[0];
        var vertices = ring.Count - 1; // the ring repeats its first position
        Assert.InRange(vertices, 12, 20);
        Assert.True(GeoService.IsInside(37.8650, 23.7530, geometry), "the municipality centre must be inside");
    }

    [Fact]
    public async Task Seeded_municipality_is_marked_approximate_with_the_plan_defaults()
    {
        var municipality = await LoadMunicipalityAsync();

        Assert.True(municipality.BoundaryIsApproximate);
        Assert.Equal("Glyfada", municipality.Name);
        Assert.Equal(37.8650, municipality.CenterLatitude);
        Assert.Equal(23.7530, municipality.CenterLongitude);
        Assert.Equal(14, municipality.DefaultZoom);
        Assert.Equal("Europe/Athens", municipality.TimeZoneId);
        Assert.Equal(1200, municipality.StreetlightCount);
        Assert.Equal(60, municipality.StreetlightWatts);
    }

    [Fact]
    public async Task Every_seeded_bin_is_inside_the_seeded_boundary()
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        await WasteBinSeed.SeedWasteBinsAsync(context);

        var boundary = GeoService.ParseGeoJson(MunicipalitySeed.ApproximateBoundaryGeoJson);
        var bins = await context.WasteBins.ToListAsync();

        Assert.InRange(bins.Count, 20, 28);
        foreach (var bin in bins)
        {
            Assert.True(GeoService.IsInside(bin.Latitude, bin.Longitude, boundary),
                $"bin {bin.Id} at {bin.Latitude},{bin.Longitude} is outside the seeded boundary");
            Assert.InRange(bin.Latitude, 37.855, 37.875);
            Assert.InRange(bin.Longitude, 23.745, 23.765);
        }
    }

    [Fact]
    public async Task Waste_bin_seed_only_runs_when_the_table_is_empty()
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        await WasteBinSeed.SeedWasteBinsAsync(context);
        var first = await context.WasteBins.CountAsync();
        await WasteBinSeed.SeedWasteBinsAsync(context);

        Assert.Equal(first, await context.WasteBins.CountAsync());
    }

    [Fact]
    public void Search_keeps_only_administrative_polygons_and_drops_the_rest()
    {
        var candidates = Lookup_search(Responding(SearchFixture));

        Assert.Equal(2, candidates.Count);
        Assert.Contains(candidates, c => c.OsmType == "R" && c.OsmId == 1234 && c.DisplayName == "Glyfada, Attica, Greece");
        Assert.Contains(candidates, c => c.OsmType == "W" && c.OsmId == 5);
        Assert.DoesNotContain(candidates, c => c.OsmId == 77 || c.OsmId == 99);
    }

    [Fact]
    public async Task Place_only_search_resolves_the_containing_municipality_by_reverse_lookup()
    {
        var handler = new StubHttpHandler(request => StubHttpHandler.Json(
            request.RequestUri!.AbsolutePath == "/reverse" ? RelationJson(1234) : $"[{PlaceJson(77)}]"));

        var candidates = await CreateLookup(handler).SearchAsync("Glyfada, Attica, Greece");

        var candidate = Assert.Single(candidates);
        Assert.Equal("R", candidate.OsmType);
        Assert.Equal(1234, candidate.OsmId);
        Assert.Equal(2, handler.Requests.Count);
        var reverse = handler.Requests[1].RequestUri!;
        Assert.Equal("/reverse", reverse.AbsolutePath);
        Assert.Contains("zoom=10", reverse.Query);
        Assert.Contains("lat=37.86", reverse.Query);
    }

    [Fact]
    public async Task Reverse_results_that_repeat_a_boundary_collapse_to_one_candidate()
    {
        var handler = new StubHttpHandler(request => StubHttpHandler.Json(
            request.RequestUri!.AbsolutePath == "/reverse" ? RelationJson(1234) : $"[{PlaceJson(77)},{PlaceJson(78)}]"));

        var candidates = await CreateLookup(handler).SearchAsync("Glyfada");

        Assert.Single(candidates);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Reverse_lookups_are_capped_at_three_per_search()
    {
        var reverses = 0;
        var places = string.Join(",", Enumerable.Range(0, 5).Select(i => PlaceJson(77 + i)));
        var handler = new StubHttpHandler(request => request.RequestUri!.AbsolutePath == "/reverse"
            ? StubHttpHandler.Json(RelationJson(1000 + ++reverses))
            : StubHttpHandler.Json($"[{places}]"));

        var candidates = await CreateLookup(handler).SearchAsync("Glyfada");

        Assert.Equal(3, candidates.Count);
        Assert.Equal(4, handler.Requests.Count);
    }

    private const string PlaceTemplate =
        """{"place_id":10,"osm_type":"node","osm_id":OSMID,"lat":"37.8600","lon":"23.7500","category":"place","type":"suburb","name":"Glyfada","display_name":"Glyfada, Attica, Greece","geojson":{"type":"Point","coordinates":[23.75,37.86]}}""";

    private const string RelationTemplate =
        """{"place_id":2,"osm_type":"relation","osm_id":OSMID,"lat":"37.8650","lon":"23.7530","category":"boundary","type":"administrative","name":"Glyfada","display_name":"Glyfada, Attica, Greece","geojson":{"type":"Polygon","coordinates":[[[23.73,37.87],[23.79,37.87],[23.79,37.84],[23.73,37.84],[23.73,37.87]]]}}""";

    private static string PlaceJson(long osmId) => PlaceTemplate.Replace("OSMID", osmId.ToString());

    private static string RelationJson(long osmId) => RelationTemplate.Replace("OSMID", osmId.ToString());

    [Fact]
    public async Task Requests_carry_a_generic_user_agent_and_no_personal_data()
    {
        var handler = Responding(SearchFixture);
        var lookup = CreateLookup(handler);

        await lookup.SearchAsync("Glyfada, Attica, Greece");

        // The fixture's place node also triggers a reverse lookup, so check the search request (the first one).
        Assert.True(handler.Requests.Count >= 1);
        var request = handler.Requests[0];
        var userAgent = request.Headers.UserAgent.ToString();
        Assert.Contains("SmartTown-Municipality-Platform/1.0", userAgent);
        Assert.DoesNotContain("@", userAgent);
        Assert.DoesNotContain("github", userAgent, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("nominatim.openstreetmap.org", request.RequestUri.Host);
        Assert.Contains("polygon_threshold=0.0005", request.RequestUri.Query);
        Assert.Contains("format=jsonv2", request.RequestUri.Query);
    }

    [Fact]
    public async Task Lookup_failure_throws_a_boundary_lookup_exception()
    {
        var lookup = CreateLookup(Responding("{}", System.Net.HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<BoundaryLookupException>(() => lookup.SearchAsync("Glyfada"));
    }

    [Fact]
    public async Task Lookup_of_a_non_boundary_object_returns_null()
    {
        var lookup = CreateLookup(Responding("[]"));

        var candidate = await lookup.LookupAsync("N", 77);

        Assert.Null(candidate);
    }

    [Fact]
    public async Task Apply_stores_the_boundary_centre_and_source_and_clears_approximate()
    {
        var lookup = CreateLookup(Responding(LookupFixture));
        var candidate = await lookup.LookupAsync("R", 1234);
        Assert.NotNull(candidate);

        using (var scope = _provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IMunicipalityService>().ApplyBoundaryAsync(candidate);
        }

        var municipality = await LoadMunicipalityAsync();
        Assert.False(municipality.BoundaryIsApproximate);
        Assert.Equal("OpenStreetMap relation 1234", municipality.BoundarySource);
        Assert.NotNull(municipality.BoundaryFetchedAt);
        Assert.Equal(37.8650, municipality.CenterLatitude);
    }

    [Fact]
    public async Task Auto_fetch_applies_the_relation_when_the_boundary_is_still_approximate()
    {
        var handler = Responding(SearchFixture);
        var stored = await FetchOnceAsync(handler, Config());

        Assert.True(stored);
        var municipality = await LoadMunicipalityAsync();
        Assert.False(municipality.BoundaryIsApproximate);
        Assert.Equal("OpenStreetMap relation 1234", municipality.BoundarySource);
        // The search, then one reverse lookup for the fixture's place node.
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("Glyfada", Uri.UnescapeDataString(handler.Requests[0].RequestUri.Query));
    }

    [Fact]
    public async Task Auto_fetch_runs_once_and_does_nothing_after_success()
    {
        var handler = Responding(SearchFixture);
        var provider = BuildHostedProvider(handler);
        var scopes = provider.GetRequiredService<IServiceScopeFactory>();

        await BoundaryAutoFetchService.FetchOnceAsync(scopes, Config(), NullLogger.Instance);
        var requestsAfterFirst = handler.Requests.Count;
        var second = await BoundaryAutoFetchService.FetchOnceAsync(scopes, Config(), NullLogger.Instance);

        Assert.False(second);
        Assert.Equal(requestsAfterFirst, handler.Requests.Count);
        provider.Dispose();
    }

    [Fact]
    public async Task Auto_fetch_failure_keeps_the_approximate_boundary()
    {
        var stored = await FetchOnceAsync(Responding("{}", System.Net.HttpStatusCode.ServiceUnavailable), Config());

        Assert.False(stored);
        var municipality = await LoadMunicipalityAsync();
        Assert.True(municipality.BoundaryIsApproximate);
        Assert.Equal(MunicipalitySeed.ApproximateSource, municipality.BoundarySource);
    }

    [Fact]
    public async Task Auto_fetch_with_no_candidates_keeps_the_approximate_boundary()
    {
        var stored = await FetchOnceAsync(Responding("[]"), Config());

        Assert.False(stored);
        Assert.True((await LoadMunicipalityAsync()).BoundaryIsApproximate);
    }

    [Fact]
    public async Task Auto_fetch_is_skipped_entirely_when_disabled()
    {
        var handler = Responding(SearchFixture);

        var stored = await FetchOnceAsync(handler, Config(autoFetch: false));

        Assert.False(stored);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void Chooser_prefers_a_relation_whose_name_matches()
    {
        var geometry = GeoService.ParseGeoJson(MunicipalitySeed.ApproximateBoundaryGeoJson);
        var candidates = new List<BoundaryCandidate>
        {
            new("W", 5, "Other", "Other, Greece", 37.2, 23.2, geometry),
            new("R", 9, "Something", "Something, Greece", 37.1, 23.1, geometry),
            new("R", 1234, "Glyfada", "Glyfada, Attica, Greece", 37.865, 23.753, geometry),
        };

        var chosen = BoundaryAutoFetchService.ChooseCandidate(candidates, "Glyfada");

        Assert.Equal(1234, chosen.OsmId);
    }

    [Fact]
    public async Task Invalid_boundary_upload_leaves_the_stored_row_unchanged()
    {
        var before = await LoadMunicipalityAsync();
        var bad = JsonDocument.Parse("""{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1]]]}""").RootElement.Clone();

        using (var scope = _provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IMunicipalityService>();
            await Assert.ThrowsAsync<GeoJsonException>(() => service.UpdateAsync(new MunicipalityUpdateDTO
            {
                Name = "Renamed",
                CenterLatitude = 37.8650,
                CenterLongitude = 23.7530,
                DefaultZoom = 14,
                StreetlightCount = 1200,
                StreetlightWatts = 60,
                Boundary = bad,
            }));
        }

        var after = await LoadMunicipalityAsync();
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.BoundaryGeoJson, after.BoundaryGeoJson);
    }

    private static IReadOnlyList<BoundaryCandidate> Lookup_search(StubHttpHandler handler) =>
        CreateLookup(handler).SearchAsync("Glyfada").GetAwaiter().GetResult();

    private async Task<bool> FetchOnceAsync(StubHttpHandler handler, IConfiguration config)
    {
        var provider = BuildHostedProvider(handler);
        try
        {
            return await BoundaryAutoFetchService.FetchOnceAsync(
                provider.GetRequiredService<IServiceScopeFactory>(), config, NullLogger.Instance);
        }
        finally
        {
            provider.Dispose();
        }
    }

    /// <summary>A provider that shares this test's database and a stubbed Nominatim.</summary>
    private ServiceProvider BuildHostedProvider(StubHttpHandler handler)
    {
        var services = new ServiceCollection();
        services.AddDbContext<DataContext>(o => o.UseSqlite(_connection));
        services.AddScoped<IMunicipalityService, MunicipalityService>();
        services.AddHttpClient<BoundaryLookupService>().ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddSingleton<IConfiguration>(Config());
        services.AddLogging();
        return services.BuildServiceProvider();
    }
}

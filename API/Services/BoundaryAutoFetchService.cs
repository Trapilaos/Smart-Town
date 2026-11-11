using API.Interfaces;

namespace API.Services
{
    /// <summary>
    /// One-time background fetch of the municipality boundary from OpenStreetMap on startup, but only while the
    /// stored boundary is still the approximate seed. Failures are logged and the seed is kept.
    /// </summary>
    public class BoundaryAutoFetchService : BackgroundService
    {
        public const string DefaultQuery = "Municipality of Glyfada";

        private readonly IServiceScopeFactory _scopes;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BoundaryAutoFetchService> _logger;

        public BoundaryAutoFetchService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<BoundaryAutoFetchService> logger)
        {
            _scopes = scopes;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await FetchOnceAsync(_scopes, _configuration, _logger, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host is shutting down.
            }
            catch (Exception ex)
            {
                // Startup is never blocked or failed by the boundary lookup.
                _logger.LogWarning(ex, "Automatic municipality boundary fetch failed; keeping the approximate boundary");
            }
        }

        /// <summary>Runs the fetch once. Returns true when a real boundary was stored.</summary>
        public static async Task<bool> FetchOnceAsync(
            IServiceScopeFactory scopes,
            IConfiguration configuration,
            ILogger logger,
            CancellationToken ct = default)
        {
            if (!configuration.GetValue("Municipality:AutoFetchBoundary", true)) return false;

            using var scope = scopes.CreateScope();
            var municipalities = scope.ServiceProvider.GetRequiredService<IMunicipalityService>();
            var lookup = scope.ServiceProvider.GetRequiredService<BoundaryLookupService>();

            var municipality = await municipalities.GetEntityAsync();
            if (!municipality.BoundaryIsApproximate) return false;

            var query = configuration["Municipality:BoundaryQuery"];
            if (string.IsNullOrWhiteSpace(query)) query = DefaultQuery;

            try
            {
                var candidates = await lookup.SearchAsync(query, ct);
                var chosen = ChooseCandidate(candidates, municipality.Name);
                if (chosen == null)
                {
                    logger.LogWarning("No administrative boundary found on OpenStreetMap for {Query}; keeping the approximate boundary", query);
                    return false;
                }

                await municipalities.ApplyBoundaryAsync(chosen);
                logger.LogInformation("Municipality boundary fetched from OpenStreetMap ({OsmType} {OsmId})", chosen.OsmType, chosen.OsmId);
                return true;
            }
            catch (BoundaryLookupException ex)
            {
                logger.LogWarning(ex, "Municipality boundary lookup failed for {Query}; keeping the approximate boundary", query);
                return false;
            }
        }

        /// <summary>Prefers relations, then a candidate whose name matches the municipality.</summary>
        public static BoundaryCandidate ChooseCandidate(IReadOnlyList<BoundaryCandidate> candidates, string municipalityName)
        {
            if (candidates.Count == 0) return null;

            var relations = candidates.Where(c => c.OsmType == "R").ToList();
            var pool = relations.Count > 0 ? relations : candidates.ToList();

            return pool.FirstOrDefault(c => NameMatches(c, municipalityName)) ?? pool[0];
        }

        private static bool NameMatches(BoundaryCandidate candidate, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)
                || candidate.DisplayName.StartsWith(name + ",", StringComparison.OrdinalIgnoreCase);
        }
    }
}

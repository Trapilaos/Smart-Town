using System.Globalization;
using System.Text.Json;
using API.Geo;

namespace API.Services
{
    /// <summary>A validated administrative boundary from OpenStreetMap.</summary>
    public sealed record BoundaryCandidate(
        string OsmType,
        long OsmId,
        string Name,
        string DisplayName,
        double CenterLat,
        double CenterLng,
        GeoGeometry Geometry);

    /// <summary>The OpenStreetMap lookup failed (network, timeout, HTTP error or malformed response).</summary>
    public sealed class BoundaryLookupException : Exception
    {
        public BoundaryLookupException(string message, Exception inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// Queries OpenStreetMap Nominatim for administrative boundaries. Requests are spaced by at least
    /// Municipality:NominatimMinIntervalMs (default 1000 ms, the Nominatim usage policy limit).
    /// </summary>
    public class BoundaryLookupService
    {
        public const string BaseUrl = "https://nominatim.openstreetmap.org/";
        public const string UserAgent = "SmartTown-Municipality-Platform/1.0";
        public const int CandidateLimit = 5;
        /// <summary>At most this many place results are resolved to their containing boundary per search.</summary>
        public const int MaxReverseLookups = 3;
        private const string PolygonOptions = "polygon_geojson=1&polygon_threshold=0.0005";

        // Shared by every instance: Nominatim's limit applies to the whole process, not one client.
        private static readonly SemaphoreSlim RequestGate = new(1, 1);
        private static DateTime _lastRequestUtc = DateTime.MinValue;

        private readonly HttpClient _http;
        private readonly TimeSpan _minInterval;
        private readonly ILogger<BoundaryLookupService> _logger;

        public BoundaryLookupService(HttpClient http, IConfiguration configuration, ILogger<BoundaryLookupService> logger)
        {
            _http = http;
            _logger = logger;
            _minInterval = TimeSpan.FromMilliseconds(Math.Max(0, configuration.GetValue("Municipality:NominatimMinIntervalMs", 1000)));

            // A generic identifier only: no personal data, contact address or repository URL.
            _http.BaseAddress ??= new Uri(BaseUrl);
            _http.Timeout = TimeSpan.FromSeconds(10);
            _http.DefaultRequestHeaders.UserAgent.Clear();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            _http.DefaultRequestHeaders.AcceptLanguage.Clear();
            _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");
        }

        /// <summary>Returns up to <see cref="CandidateLimit"/> administrative boundary polygons matching the query.</summary>
        public async Task<IReadOnlyList<BoundaryCandidate>> SearchAsync(string query, CancellationToken ct = default)
        {
            var url = $"search?q={Uri.EscapeDataString(query)}&format=jsonv2&{PolygonOptions}&limit={CandidateLimit}&addressdetails=0";
            using var document = await GetJsonAsync(url, ct);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new BoundaryLookupException("Boundary lookup returned an unexpected response");

            var candidates = new List<BoundaryCandidate>();
            var seen = new HashSet<(string OsmType, long OsmId)>();
            var places = new List<JsonElement>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var candidate = TryParseCandidate(item);
                if (candidate != null)
                {
                    if (seen.Add((candidate.OsmType, candidate.OsmId))) candidates.Add(candidate);
                }
                else if ((ReadString(item, "category") ?? ReadString(item, "class")) != "boundary")
                {
                    places.Add(item);
                }
            }

            // Natural queries such as "Glyfada, Attica, Greece" often match a place (a suburb or town node) rather
            // than the municipality. Ask which municipality-level boundary contains the first few of those places.
            foreach (var place in places.Take(MaxReverseLookups))
            {
                if (candidates.Count >= CandidateLimit) break;
                if (!TryParseCoordinate(ReadString(place, "lat"), out var lat) || !TryParseCoordinate(ReadString(place, "lon"), out var lng))
                    continue;

                var containing = await ReverseAsync(lat, lng, ct);
                if (containing != null && seen.Add((containing.OsmType, containing.OsmId))) candidates.Add(containing);
            }
            return candidates;
        }

        /// <summary>The municipality-level boundary (zoom 10) that contains a point. Null when there is none.</summary>
        private async Task<BoundaryCandidate> ReverseAsync(double lat, double lng, CancellationToken ct)
        {
            var latText = lat.ToString(CultureInfo.InvariantCulture);
            var lngText = lng.ToString(CultureInfo.InvariantCulture);
            var url = $"reverse?lat={latText}&lon={lngText}&zoom=10&format=jsonv2&{PolygonOptions}";
            using var document = await GetJsonAsync(url, ct);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            return TryParseCandidate(document.RootElement);
        }

        /// <summary>Fetches one exact OSM object. Returns null when it is not an administrative boundary polygon.</summary>
        public async Task<BoundaryCandidate> LookupAsync(string osmType, long osmId, CancellationToken ct = default)
        {
            var letter = osmType.ToUpperInvariant();
            var url = $"lookup?osm_ids={letter}{osmId.ToString(CultureInfo.InvariantCulture)}&format=jsonv2&{PolygonOptions}";
            using var document = await GetJsonAsync(url, ct);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return null;

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var candidate = TryParseCandidate(item);
                if (candidate != null && candidate.OsmType == letter && candidate.OsmId == osmId) return candidate;
            }
            return null;
        }

        /// <summary>
        /// Keeps only administrative boundaries (class/category "boundary", type "administrative") whose geometry
        /// is a Polygon or MultiPolygon and passes the same validation as uploads. Anything else is dropped.
        /// </summary>
        public static BoundaryCandidate TryParseCandidate(JsonElement item)
        {
            if (item.ValueKind != JsonValueKind.Object) return null;

            var category = ReadString(item, "category") ?? ReadString(item, "class");
            if (category != "boundary" || ReadString(item, "type") != "administrative") return null;

            var osmType = ReadString(item, "osm_type") switch
            {
                "relation" => "R",
                "way" => "W",
                "node" => "N",
                _ => null,
            };
            if (osmType == null || !item.TryGetProperty("osm_id", out var idElement) || idElement.ValueKind != JsonValueKind.Number)
                return null;
            if (!idElement.TryGetInt64(out var osmId)) return null;

            if (!item.TryGetProperty("geojson", out var geojson)) return null;
            GeoGeometry geometry;
            try
            {
                geometry = GeoService.ParseGeometry(geojson);
                if (GeoService.ToGeoJson(geometry).Length > GeoService.MaxGeoJsonBytes) return null;
            }
            catch (GeoJsonException)
            {
                return null;
            }

            if (!TryParseCoordinate(ReadString(item, "lat"), out var lat) || !TryParseCoordinate(ReadString(item, "lon"), out var lng))
                return null;

            return new BoundaryCandidate(
                osmType,
                osmId,
                ReadString(item, "name") ?? string.Empty,
                ReadString(item, "display_name") ?? string.Empty,
                lat,
                lng,
                geometry);
        }

        private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
        {
            await RequestGate.WaitAsync(ct);
            try
            {
                var wait = _minInterval - (DateTime.UtcNow - _lastRequestUtc);
                if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
                _lastRequestUtc = DateTime.UtcNow;

                HttpResponseMessage response;
                try
                {
                    response = await _http.GetAsync(url, ct);
                }
                catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
                {
                    throw new BoundaryLookupException("Boundary lookup timed out", ex);
                }
                catch (HttpRequestException ex)
                {
                    throw new BoundaryLookupException("Boundary lookup is unavailable", ex);
                }

                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Boundary lookup returned HTTP {Status}", (int)response.StatusCode);
                        throw new BoundaryLookupException($"Boundary lookup returned HTTP {(int)response.StatusCode}");
                    }

                    var body = await response.Content.ReadAsStringAsync(ct);
                    try
                    {
                        return JsonDocument.Parse(body);
                    }
                    catch (JsonException ex)
                    {
                        throw new BoundaryLookupException("Boundary lookup returned invalid JSON", ex);
                    }
                }
            }
            finally
            {
                RequestGate.Release();
            }
        }

        private static string ReadString(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static bool TryParseCoordinate(string text, out double value) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value);
    }
}

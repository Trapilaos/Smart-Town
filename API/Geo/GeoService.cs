using System.Text;
using System.Text.Json;

namespace API.Geo
{
    /// <summary>
    /// Pure geometry helpers: GeoJSON parsing and validation, point-in-polygon and great-circle distance.
    /// </summary>
    public static class GeoService
    {
        public const int MaxGeoJsonBytes = 2 * 1024 * 1024;
        public const double EarthRadiusMeters = 6_371_008.8;

        /// <summary>Great-circle distance in metres between two lat/lng points.</summary>
        public static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        public static double HaversineMeters(LatLng a, LatLng b) => HaversineMeters(a.Lat, a.Lng, b.Lat, b.Lng);

        /// <summary>True when the point is inside the geometry. Holes are excluded; MultiPolygons match any member.</summary>
        public static bool IsInside(double lat, double lng, GeoGeometry geometry)
        {
            ArgumentNullException.ThrowIfNull(geometry);
            foreach (var polygon in geometry.Polygons)
            {
                if (PolygonContains(polygon, lat, lng)) return true;
            }
            return false;
        }

        private static bool PolygonContains(GeoPolygon polygon, double lat, double lng)
        {
            if (polygon.Rings.Count == 0 || !RingContains(polygon.Rings[0], lat, lng)) return false;
            for (var i = 1; i < polygon.Rings.Count; i++)
            {
                if (RingContains(polygon.Rings[i], lat, lng)) return false; // inside a hole
            }
            return true;
        }

        /// <summary>Even-odd ray casting. The ray runs towards +lng; x = lng, y = lat.</summary>
        private static bool RingContains(IReadOnlyList<GeoPoint> ring, double lat, double lng)
        {
            var inside = false;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            {
                var pi = ring[i];
                var pj = ring[j];
                if ((pi.Lat > lat) != (pj.Lat > lat) &&
                    lng < (pj.Lng - pi.Lng) * (lat - pi.Lat) / (pj.Lat - pi.Lat) + pi.Lng)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Parses a GeoJSON Polygon, MultiPolygon, Feature or FeatureCollection (Polygon/MultiPolygon features only)
        /// and returns the normalised geometry. Throws <see cref="GeoJsonException"/> with an admin-facing message.
        /// </summary>
        public static GeoGeometry ParseGeoJson(string geoJson)
        {
            if (string.IsNullOrWhiteSpace(geoJson)) throw new GeoJsonException("Boundary GeoJSON is required");
            if (Encoding.UTF8.GetByteCount(geoJson) > MaxGeoJsonBytes)
                throw new GeoJsonException("Boundary GeoJSON must be 2 MB or smaller");

            using var document = ParseDocument(geoJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new GeoJsonException("Boundary must be a GeoJSON object");

            var type = ReadType(root, "GeoJSON");
            switch (type)
            {
                case "Polygon":
                case "MultiPolygon":
                    return ParseGeometry(root);

                case "Feature":
                    if (!root.TryGetProperty("geometry", out var featureGeometry) || featureGeometry.ValueKind != JsonValueKind.Object)
                        throw new GeoJsonException("Feature must have a geometry");
                    return ParseGeometry(featureGeometry);

                case "FeatureCollection":
                    return ParseFeatureCollection(root);

                default:
                    throw new GeoJsonException("Boundary must be a Polygon, MultiPolygon, Feature or FeatureCollection");
            }
        }

        /// <summary>Parses a GeoJSON geometry object (type Polygon or MultiPolygon) held in a JsonElement.</summary>
        public static GeoGeometry ParseGeometry(JsonElement geometry)
        {
            if (geometry.ValueKind != JsonValueKind.Object) throw new GeoJsonException("Geometry must be an object");
            var type = ReadType(geometry, "Geometry");
            if (!geometry.TryGetProperty("coordinates", out var coordinates) || coordinates.ValueKind != JsonValueKind.Array)
                throw new GeoJsonException("Geometry must have coordinates");

            var polygons = new List<GeoPolygon>();
            switch (type)
            {
                case "Polygon":
                    polygons.Add(ParsePolygon(coordinates));
                    break;
                case "MultiPolygon":
                    foreach (var polygon in coordinates.EnumerateArray())
                    {
                        if (polygon.ValueKind != JsonValueKind.Array) throw new GeoJsonException("MultiPolygon coordinates are malformed");
                        polygons.Add(ParsePolygon(polygon));
                    }
                    break;
                default:
                    throw new GeoJsonException("Geometry must be a Polygon or MultiPolygon");
            }

            if (polygons.Count == 0) throw new GeoJsonException("Geometry has no polygons");
            return new GeoGeometry(polygons);
        }

        /// <summary>Serialises a geometry as a plain GeoJSON Polygon or MultiPolygon string.</summary>
        public static string ToGeoJson(GeoGeometry geometry)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                if (geometry.Polygons.Count == 1)
                {
                    writer.WriteString("type", "Polygon");
                    writer.WritePropertyName("coordinates");
                    WritePolygon(writer, geometry.Polygons[0]);
                }
                else
                {
                    writer.WriteString("type", "MultiPolygon");
                    writer.WriteStartArray("coordinates");
                    foreach (var polygon in geometry.Polygons) WritePolygon(writer, polygon);
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        public static JsonElement ToJsonElement(GeoGeometry geometry)
        {
            using var document = JsonDocument.Parse(ToGeoJson(geometry));
            return document.RootElement.Clone();
        }

        private static void WritePolygon(Utf8JsonWriter writer, GeoPolygon polygon)
        {
            writer.WriteStartArray();
            foreach (var ring in polygon.Rings)
            {
                writer.WriteStartArray();
                foreach (var point in ring)
                {
                    writer.WriteStartArray();
                    writer.WriteNumberValue(point.Lng);
                    writer.WriteNumberValue(point.Lat);
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
        }

        private static GeoGeometry ParseFeatureCollection(JsonElement root)
        {
            if (!root.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                throw new GeoJsonException("FeatureCollection must have a features array");

            var polygons = new List<GeoPolygon>();
            foreach (var feature in features.EnumerateArray())
            {
                if (feature.ValueKind != JsonValueKind.Object || ReadType(feature, "Feature") != "Feature")
                    throw new GeoJsonException("FeatureCollection may only contain Feature objects");
                if (!feature.TryGetProperty("geometry", out var geometry) || geometry.ValueKind != JsonValueKind.Object)
                    throw new GeoJsonException("Feature must have a geometry");

                polygons.AddRange(ParseGeometry(geometry).Polygons);
            }

            if (polygons.Count == 0) throw new GeoJsonException("FeatureCollection contains no polygons");
            return new GeoGeometry(polygons);
        }

        private static GeoPolygon ParsePolygon(JsonElement rings)
        {
            var parsed = new List<IReadOnlyList<GeoPoint>>();
            foreach (var ring in rings.EnumerateArray())
            {
                if (ring.ValueKind != JsonValueKind.Array) throw new GeoJsonException("Polygon rings must be arrays");
                parsed.Add(ParseRing(ring));
            }
            if (parsed.Count == 0) throw new GeoJsonException("Polygon must have at least one ring");
            return new GeoPolygon(parsed);
        }

        private static IReadOnlyList<GeoPoint> ParseRing(JsonElement ring)
        {
            var points = new List<GeoPoint>();
            foreach (var position in ring.EnumerateArray())
            {
                if (position.ValueKind != JsonValueKind.Array || position.GetArrayLength() < 2)
                    throw new GeoJsonException("Each position must be an array of [lng, lat]");

                var lng = position[0];
                var lat = position[1];
                if (lng.ValueKind != JsonValueKind.Number || lat.ValueKind != JsonValueKind.Number)
                    throw new GeoJsonException("Positions must contain numbers");

                var point = new GeoPoint(lng.GetDouble(), lat.GetDouble());
                if (double.IsNaN(point.Lat) || double.IsNaN(point.Lng) || point.Lat < -90 || point.Lat > 90 || point.Lng < -180 || point.Lng > 180)
                    throw new GeoJsonException("Coordinates must be within lat [-90, 90] and lng [-180, 180]");

                points.Add(point);
            }

            if (points.Count < 4) throw new GeoJsonException("Each ring needs at least 4 positions");
            if (points[0] != points[^1]) throw new GeoJsonException("Each ring must be closed (first and last position equal)");
            return points;
        }

        private static string ReadType(JsonElement element, string what)
        {
            if (!element.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String)
                throw new GeoJsonException($"{what} must have a type");
            return type.GetString();
        }

        private static JsonDocument ParseDocument(string geoJson)
        {
            try
            {
                return JsonDocument.Parse(geoJson);
            }
            catch (JsonException)
            {
                throw new GeoJsonException("Boundary is not valid JSON");
            }
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}

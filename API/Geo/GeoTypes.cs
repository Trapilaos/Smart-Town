namespace API.Geo
{
    /// <summary>A GeoJSON position: longitude first, as in the GeoJSON spec.</summary>
    public readonly record struct GeoPoint(double Lng, double Lat);

    /// <summary>A latitude/longitude pair used by the API (depot, bins, stops).</summary>
    public readonly record struct LatLng(double Lat, double Lng);

    /// <summary>One polygon: Rings[0] is the outer ring, the rest are holes. Every ring is closed.</summary>
    public sealed record GeoPolygon(IReadOnlyList<IReadOnlyList<GeoPoint>> Rings);

    /// <summary>A normalised Polygon or MultiPolygon.</summary>
    public sealed record GeoGeometry(IReadOnlyList<GeoPolygon> Polygons);

    /// <summary>Raised when GeoJSON is malformed, out of range or too large. The message is safe to show to admins.</summary>
    public sealed class GeoJsonException : Exception
    {
        public GeoJsonException(string message) : base(message) { }
    }
}

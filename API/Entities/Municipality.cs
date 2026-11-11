namespace API.Entities
{
    /// <summary>The municipality settings. The table holds a single row (Id = 1).</summary>
    public class Municipality
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public int DefaultZoom { get; set; }
        public string TimeZoneId { get; set; }
        /// <summary>Normalised GeoJSON Polygon or MultiPolygon text.</summary>
        public string BoundaryGeoJson { get; set; }
        public bool BoundaryIsApproximate { get; set; }
        public string BoundarySource { get; set; }
        public DateTime? BoundaryFetchedAt { get; set; }
        public int StreetlightCount { get; set; }
        public int StreetlightWatts { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

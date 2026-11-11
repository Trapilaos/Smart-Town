using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace API.DTOs
{
    /// <summary>Public municipality settings. The boundary is returned as a GeoJSON geometry object.</summary>
    public class MunicipalityDTO
    {
        public string Name { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public int DefaultZoom { get; set; }
        public string TimeZoneId { get; set; }
        public JsonElement Boundary { get; set; }
        public bool BoundaryIsApproximate { get; set; }
        public string BoundarySource { get; set; }
        public DateTime? BoundaryFetchedAt { get; set; }
        public int StreetlightCount { get; set; }
        public int StreetlightWatts { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Admin update. Omitting Boundary keeps the current boundary.</summary>
    public class MunicipalityUpdateDTO
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; }

        [Range(-90, 90)]
        public double CenterLatitude { get; set; }

        [Range(-180, 180)]
        public double CenterLongitude { get; set; }

        [Range(1, 20)]
        public int DefaultZoom { get; set; }

        [Range(0, 100000)]
        public int StreetlightCount { get; set; }

        [Range(1, 1000)]
        public int StreetlightWatts { get; set; }

        /// <summary>Optional GeoJSON (Polygon, MultiPolygon, Feature or FeatureCollection).</summary>
        public JsonElement? Boundary { get; set; }
    }

    public class BoundarySearchDTO
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Query { get; set; }
    }

    public class BoundaryApplyDTO
    {
        /// <summary>OSM object type: N (node), W (way) or R (relation). Only relations are normally administrative boundaries.</summary>
        [Required]
        [RegularExpression("^(?i)[NWR]$", ErrorMessage = "OsmType must be N, W or R")]
        public string OsmType { get; set; }

        [Range(1, long.MaxValue)]
        public long OsmId { get; set; }
    }

    /// <summary>A boundary candidate returned by the OpenStreetMap search. Geometry is a GeoJSON geometry object.</summary>
    public record BoundaryCandidateDTO(
        string OsmType,
        long OsmId,
        string DisplayName,
        double CenterLat,
        double CenterLng,
        JsonElement Geometry);
}

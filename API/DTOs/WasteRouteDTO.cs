namespace API.DTOs
{
    public record PointDTO(double Lat, double Lng);

    public class RouteStopDTO
    {
        public int BinId { get; set; }
        public string Location { get; set; }
        public string Type { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double FillPercent { get; set; }
        /// <summary>1-based position in the collection order.</summary>
        public int Order { get; set; }
    }

    public class WasteRouteDTO
    {
        public List<RouteStopDTO> Stops { get; set; } = new();
        /// <summary>Depot to the first stop, between stops, and back to the depot.</summary>
        public double TotalDistanceKm { get; set; }
        public PointDTO Depot { get; set; }
    }
}

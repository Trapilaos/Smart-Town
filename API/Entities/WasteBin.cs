namespace API.Entities
{
    public enum WasteType
    {
        General,
        Recycling,
        Glass,
        Organic,
        Paper,
    }

    public class WasteBin
    {
        public int Id { get; set; }
        /// <summary>Address label, for display only.</summary>
        public string Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public WasteType Type { get; set; } = WasteType.General;
        public int Capacity { get; set; } // Capacity in liters
        public int CurrentFillLevel { get; set; } // Current fill level in liters
        public DateTime LastEmptied { get; set; }
        /// <summary>False after a soft delete.</summary>
        public bool IsActive { get; set; } = true;
    }
}

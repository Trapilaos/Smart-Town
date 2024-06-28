namespace API.Entities
{
    public class WasteBin
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public int Capacity { get; set; } // Capacity in liters
        public int CurrentFillLevel { get; set; } // Current fill level in liters
        public DateTime LastEmptied { get; set; }
    }
}

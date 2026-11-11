namespace API.Entities
{
    public enum ParkingType
    {
        Street,
        Lot,
        Disabled,
        EV,
    }

    public class ParkingSpace
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        /// <summary>Stored as text: Street, Lot, Disabled or EV.</summary>
        public ParkingType Type { get; set; } = ParkingType.Street;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int MaxVehicles { get; set; }
        public int CurrentVehicles { get; set; }
        public decimal HourlyRate { get; set; }
        /// <summary>False after a soft delete. Inactive spaces cannot be reserved.</summary>
        public bool IsActive { get; set; } = true;
    }
}

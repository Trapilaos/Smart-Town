namespace API.DTOs
{
    public class ReservationDTO
    {
        public int ParkingSpaceId { get; set; }
        public DateTime ReservationTime { get; set; }
        public int Duration { get; set; } // Duration in minutes
    }
}
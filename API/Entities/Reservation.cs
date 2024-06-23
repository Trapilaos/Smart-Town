
namespace API.Entities
{
    public class Reservation
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ParkingSpaceId { get; set; }
        public DateTime ReservationTime { get; set; }
        public int Duration { get; set; }
    }
}
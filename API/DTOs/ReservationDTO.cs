using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class ReservationDTO
    {
        [Range(1, int.MaxValue)]
        public int ParkingSpaceId { get; set; }

        public DateTime ReservationTime { get; set; }

        /// <summary>Duration in minutes (1 to 1440).</summary>
        [Range(1, 1440)]
        public int Duration { get; set; }
    }
}

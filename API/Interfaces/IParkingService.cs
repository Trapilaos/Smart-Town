using API.Entities;

namespace API.Interfaces
{
    public interface IParkingService
    {
        Task<List<ParkingSpace>> GetParkingSpacesAsync();
        Task<(bool Success, DateTime? StartTime, DateTime? EndTime)> ReserveParkingSpaceAsync(string userId, int parkingSpaceId, DateTime reservationTime, int duration);
        Task<Reservation> GetActiveReservationAsync(string userId); // Add this method
    }
}

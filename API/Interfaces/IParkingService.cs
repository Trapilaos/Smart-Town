using API.Entities;

namespace API.Interfaces
{
    public interface IParkingService
    {
        Task<List<ParkingSpace>> GetParkingSpacesAsync();
        Task<bool> ReserveParkingSpaceAsync(string userId, int parkingSpaceId, DateTime reservationTime, int duration);
    }
}

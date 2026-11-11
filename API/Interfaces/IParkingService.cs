using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IParkingService
    {
        /// <summary>Active spaces with live availability. Public.</summary>
        Task<List<ParkingSpaceDTO>> GetParkingSpacesAsync();
        Task<(bool Success, DateTime? StartTime, DateTime? EndTime)> ReserveParkingSpaceAsync(string userId, int parkingSpaceId, DateTime reservationTime, int duration);
        Task<Reservation> GetActiveReservationAsync(string userId);

        /// <summary>Every space, including inactive ones. Admin only.</summary>
        Task<List<ParkingSpaceDTO>> GetAllForAdminAsync();
        /// <summary>Throws <see cref="API.Errors.DomainValidationException"/> when the point is outside the boundary.</summary>
        Task<ParkingSpaceDTO> CreateAsync(ParkingSpaceInputDTO input);
        /// <summary>Returns null when the space does not exist.</summary>
        Task<ParkingSpaceDTO> UpdateAsync(int id, ParkingSpaceInputDTO input);
        /// <summary>Soft delete. Returns false when the space does not exist.</summary>
        Task<bool> DeactivateAsync(int id);
        /// <summary>Reservations that have not ended yet, usernames only. Returns null when the space does not exist.</summary>
        Task<List<ParkingReservationDTO>> GetReservationsAsync(int id);
    }
}

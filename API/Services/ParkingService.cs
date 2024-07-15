using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class ParkingService : IParkingService
    {
        private readonly DataContext _context;

        public ParkingService(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all parking spaces and cleans up old reservations.
        /// </summary>
        /// <returns>A list of parking spaces.</returns>
        public async Task<List<ParkingSpace>> GetParkingSpacesAsync()
        {
            // Clean up old reservations
            await CleanUpReservationsAsync();

            return await _context.ParkingSpaces.ToListAsync();
        }

        /// <summary>
        /// Reserves a parking space for a user.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <param name="parkingSpaceId">The parking space's ID.</param>
        /// <param name="reservationTime">The reservation time.</param>
        /// <param name="duration">The reservation duration in minutes.</param>
        /// <returns>True if the reservation was successful, false otherwise.</returns>
        public async Task<bool> ReserveParkingSpaceAsync(string userId, int parkingSpaceId, DateTime reservationTime, int duration)
        {
            var parkingSpace = await _context.ParkingSpaces.FindAsync(parkingSpaceId);
            if (parkingSpace == null || parkingSpace.CurrentVehicles >= parkingSpace.MaxVehicles)
            {
                return false;
            }

            // Check for overlapping reservations
            var existingReservations = await _context.Reservations
                .Where(r => r.ParkingSpaceId == parkingSpaceId &&
                            (r.ReservationTime < reservationTime && r.ReservationTime.AddMinutes(r.Duration) > reservationTime ||
                             r.ReservationTime < reservationTime.AddMinutes(duration) && r.ReservationTime.AddMinutes(r.Duration) > reservationTime.AddMinutes(duration)))
                .ToListAsync();

            if (existingReservations.Any())
            {
                return false;
            }

            var reservation = new Reservation
            {
                UserId = userId,
                ParkingSpaceId = parkingSpaceId,
                ReservationTime = reservationTime,
                Duration = duration
            };

            parkingSpace.CurrentVehicles++;
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Schedule a task to decrement vehicles after the reservation duration
            _ = DecrementVehicleAfterDuration(parkingSpaceId, duration);

            return true;
        }

        /// <summary>
        /// Cleans up expired reservations and decrements the current vehicles in the parking spaces.
        /// </summary>
        private async Task CleanUpReservationsAsync()
        {
            var expiredReservations = await _context.Reservations
                .Where(r => r.ReservationTime.AddMinutes(r.Duration) < DateTime.Now)
                .ToListAsync();

            foreach (var reservation in expiredReservations)
            {
                var parkingSpace = await _context.ParkingSpaces.FindAsync(reservation.ParkingSpaceId);
                if (parkingSpace != null)
                {
                    parkingSpace.CurrentVehicles--;
                }

                _context.Reservations.Remove(reservation);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Decrements the current vehicles in a parking space after the reservation duration.
        /// </summary>
        /// <param name="parkingSpaceId">The parking space's ID.</param>
        /// <param name="duration">The reservation duration in minutes.</param>
        private async Task DecrementVehicleAfterDuration(int parkingSpaceId, int duration)
        {
            // Wait for the duration time in milliseconds
            await Task.Delay(duration * 60 * 1000);

            var parkingSpace = await _context.ParkingSpaces.FindAsync(parkingSpaceId);
            if (parkingSpace != null)
            {
                parkingSpace.CurrentVehicles--;
                await _context.SaveChangesAsync();
            }
        }
    }
}

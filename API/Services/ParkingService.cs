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

        public async Task<List<ParkingSpace>> GetParkingSpacesAsync()
        {
            // Clean up old reservations
            await CleanUpReservationsAsync();

            return await _context.ParkingSpaces.ToListAsync();
        }

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

            return true;
        }

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
    }
}

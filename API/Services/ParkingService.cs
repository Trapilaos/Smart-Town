using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class ParkingService : IParkingService
    {
        private readonly DataContext _context;
        private readonly ILogger<ParkingService> _logger;

        public ParkingService(DataContext context, ILogger<ParkingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ParkingSpace>> GetParkingSpacesAsync()
        {
            await SyncReservationsAsync();

            return await _context.ParkingSpaces.AsNoTracking().ToListAsync();
        }

        public async Task<(bool Success, DateTime? StartTime, DateTime? EndTime)> ReserveParkingSpaceAsync(string userId, int parkingSpaceId, DateTime reservationTime, int duration)
        {
            await SyncReservationsAsync();

            var parkingSpace = await _context.ParkingSpaces.FindAsync(parkingSpaceId);
            if (parkingSpace == null)
            {
                _logger.LogInformation("Reservation rejected: parking space {Id} not found", parkingSpaceId);
                return (false, null, null);
            }

            if (parkingSpace.CurrentVehicles >= parkingSpace.MaxVehicles)
            {
                _logger.LogInformation("Reservation rejected: parking space {Id} is full", parkingSpaceId);
                return (false, null, null);
            }

            var endTime = reservationTime.AddMinutes(duration);

            // Reservations are few and short-lived, so the overlap check is done in memory
            // (SQLite cannot translate DateTime arithmetic on columns).
            var userReservations = await _context.Reservations
                .Where(r => r.UserId == userId && r.ParkingSpaceId == parkingSpaceId)
                .ToListAsync();

            if (userReservations.Any(r => r.ReservationTime < endTime && r.ReservationTime.AddMinutes(r.Duration) > reservationTime))
            {
                _logger.LogInformation("Reservation rejected: user already has an overlapping reservation");
                return (false, null, null);
            }

            _context.Reservations.Add(new Reservation
            {
                UserId = userId,
                ParkingSpaceId = parkingSpaceId,
                ReservationTime = reservationTime,
                Duration = duration
            });

            parkingSpace.CurrentVehicles++;
            await _context.SaveChangesAsync();

            return (true, reservationTime, endTime);
        }

        public async Task<Reservation> GetActiveReservationAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var reservations = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return reservations
                .Where(r => r.ReservationTime.AddMinutes(r.Duration) > now)
                .OrderBy(r => r.ReservationTime)
                .FirstOrDefault();
        }

        /// <summary>
        /// Removes expired reservations and recomputes CurrentVehicles from the reservations that are still
        /// active. Because the counter is derived (not incremented/decremented by timers), it is idempotent
        /// and needs no background work.
        /// </summary>
        private async Task SyncReservationsAsync()
        {
            var now = DateTime.UtcNow;
            var reservations = await _context.Reservations.ToListAsync();

            var expired = reservations.Where(r => r.ReservationTime.AddMinutes(r.Duration) <= now).ToList();
            _context.Reservations.RemoveRange(expired);

            var activeBySpace = reservations.Except(expired)
                .GroupBy(r => r.ParkingSpaceId)
                .ToDictionary(g => g.Key, g => g.Count());

            var spaces = await _context.ParkingSpaces.ToListAsync();
            foreach (var space in spaces)
            {
                space.CurrentVehicles = activeBySpace.GetValueOrDefault(space.Id);
            }

            await _context.SaveChangesAsync();
        }
    }
}

using API.Data;
using API.DTOs;
using API.Entities;
using API.Errors;
using API.Helpers;
using API.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API.Services
{
    public class ParkingService : IParkingService
    {
        public const string OutsideBoundaryMessage = "Parking space must be inside the municipality boundary";

        private readonly DataContext _context;
        private readonly IMunicipalityService _municipalities;
        private readonly ILogger<ParkingService> _logger;

        /// <summary>One reservation at a time per process. See ReserveParkingSpaceAsync.</summary>
        private static readonly SemaphoreSlim ReservationGate = new(1, 1);

        public ParkingService(DataContext context, IMunicipalityService municipalities, ILogger<ParkingService> logger)
        {
            _context = context;
            _municipalities = municipalities;
            _logger = logger;
        }

        public async Task<List<ParkingSpaceDTO>> GetParkingSpacesAsync()
        {
            await SyncReservationsAsync();

            var spaces = await _context.ParkingSpaces.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Id)
                .ToListAsync();

            return await ToDtosAsync(spaces);
        }

        /// <summary>
        /// Reserves a space if its peak occupancy and the user's own bookings allow it. The check and the insert
        /// run as one unit. The write lock is taken up front (BEGIN IMMEDIATE), so two bookings cannot both see
        /// the last free slot. The process-wide gate is a second guard: the app runs as a single instance, and it
        /// keeps concurrent calls from contending for the SQLite write lock.
        /// </summary>
        public async Task<(bool Success, DateTime? StartTime, DateTime? EndTime)> ReserveParkingSpaceAsync(string userId, int parkingSpaceId, DateTime reservationTime, int duration)
        {
            await ReservationGate.WaitAsync();
            try
            {
                await SyncReservationsAsync();

                await _context.Database.OpenConnectionAsync();
                var connection = (SqliteConnection)_context.Database.GetDbConnection();
                var dbTransaction = connection.BeginTransaction(IsolationLevel.Serializable, deferred: false);
                await using var transaction = await _context.Database.UseTransactionAsync(dbTransaction);
                try
                {
                    var result = await ReserveInsideTransactionAsync(userId, parkingSpaceId, reservationTime, duration);
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            finally
            {
                ReservationGate.Release();
            }
        }

        private async Task<(bool Success, DateTime? StartTime, DateTime? EndTime)> ReserveInsideTransactionAsync(string userId, int parkingSpaceId, DateTime reservationTime, int duration)
        {
            var parkingSpace = await _context.ParkingSpaces.FindAsync(parkingSpaceId);
            if (parkingSpace == null || !parkingSpace.IsActive)
            {
                _logger.LogInformation("Reservation rejected: parking space {Id} not found or inactive", parkingSpaceId);
                return (false, null, null);
            }

            var endTime = reservationTime.AddMinutes(duration);

            // All non-expired reservations for this space (any user). Loaded into memory because
            // SQLite cannot translate DateTime arithmetic on columns.
            var spaceReservations = await _context.Reservations
                .Where(r => r.ParkingSpaceId == parkingSpaceId)
                .ToListAsync();

            if (PeakOccupancy(spaceReservations, reservationTime, endTime) >= parkingSpace.MaxVehicles)
            {
                _logger.LogInformation("Reservation rejected: parking space {Id} is full for the requested window", parkingSpaceId);
                return (false, null, null);
            }

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

        public async Task<List<ParkingSpaceDTO>> GetAllForAdminAsync()
        {
            await SyncReservationsAsync();

            var spaces = await _context.ParkingSpaces.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
            return await ToDtosAsync(spaces);
        }

        public async Task<ParkingSpaceDTO> CreateAsync(ParkingSpaceInputDTO input)
        {
            await EnsureInsideBoundaryAsync(input.Latitude, input.Longitude);

            var space = new ParkingSpace();
            Apply(space, input);
            _context.ParkingSpaces.Add(space);
            await _context.SaveChangesAsync();

            return ParkingSpaceDTO.From(space, 0);
        }

        public async Task<ParkingSpaceDTO> UpdateAsync(int id, ParkingSpaceInputDTO input)
        {
            var space = await _context.ParkingSpaces.FindAsync(id);
            if (space == null) return null;

            await EnsureInsideBoundaryAsync(input.Latitude, input.Longitude);

            Apply(space, input);
            await _context.SaveChangesAsync();

            return (await ToDtosAsync(new List<ParkingSpace> { space })).Single();
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var space = await _context.ParkingSpaces.FindAsync(id);
            if (space == null) return false;

            space.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ParkingReservationDTO>> GetReservationsAsync(int id)
        {
            if (!await _context.ParkingSpaces.AnyAsync(p => p.Id == id)) return null;

            var now = DateTime.UtcNow;
            var reservations = (await _context.Reservations.AsNoTracking()
                    .Where(r => r.ParkingSpaceId == id)
                    .ToListAsync())
                .Where(r => r.ReservationTime.AddMinutes(r.Duration) > now)
                .OrderBy(r => r.ReservationTime)
                .ToList();

            var usernames = await UsernameLookup.LoadAsync(_context);

            return reservations.Select(r => new ParkingReservationDTO
            {
                Id = r.Id,
                Username = usernames.GetValueOrDefault(r.UserId) ?? "unknown",
                StartTime = r.ReservationTime,
                EndTime = r.ReservationTime.AddMinutes(r.Duration),
                IsActive = r.ReservationTime <= now,
            }).ToList();
        }

        private async Task EnsureInsideBoundaryAsync(double lat, double lng)
        {
            if (!await _municipalities.IsInsideBoundaryAsync(lat, lng))
                throw new DomainValidationException(OutsideBoundaryMessage);
        }

        private static void Apply(ParkingSpace space, ParkingSpaceInputDTO input)
        {
            space.Name = input.Name.Trim();
            space.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
            space.Type = Enum.Parse<ParkingType>(input.Type);
            space.Latitude = input.Latitude;
            space.Longitude = input.Longitude;
            space.MaxVehicles = input.MaxVehicles;
            space.HourlyRate = input.HourlyRate;
            space.IsActive = input.IsActive;
        }

        /// <summary>Occupancy now for each space: reservations that have started and not yet ended.</summary>
        private async Task<List<ParkingSpaceDTO>> ToDtosAsync(IReadOnlyCollection<ParkingSpace> spaces)
        {
            var now = DateTime.UtcNow;
            var activeNow = (await _context.Reservations.AsNoTracking()
                    .Where(r => r.ReservationTime <= now)
                    .ToListAsync())
                .Where(r => r.ReservationTime.AddMinutes(r.Duration) > now)
                .GroupBy(r => r.ParkingSpaceId)
                .ToDictionary(g => g.Key, g => g.Count());

            return spaces.Select(s => ParkingSpaceDTO.From(s, activeNow.GetValueOrDefault(s.Id))).ToList();
        }

        /// <summary>
        /// The largest number of reservations parked at the same moment inside [start, end).
        /// Occupancy only rises when a reservation begins, so it is enough to sample the window start
        /// and every reservation start that falls inside the window. Intervals are half-open, so a
        /// booking that ends exactly when the new one starts does not overlap it.
        /// </summary>
        private static int PeakOccupancy(IReadOnlyCollection<Reservation> reservations, DateTime start, DateTime end)
        {
            var overlapping = reservations
                .Where(r => r.ReservationTime < end && r.ReservationTime.AddMinutes(r.Duration) > start)
                .ToList();

            var samplePoints = overlapping
                .Select(r => r.ReservationTime)
                .Where(t => t > start)
                .Append(start);

            return samplePoints
                .Select(t => overlapping.Count(r => r.ReservationTime <= t && t < r.ReservationTime.AddMinutes(r.Duration)))
                .DefaultIfEmpty(0)
                .Max();
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

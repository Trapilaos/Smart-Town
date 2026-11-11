using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class ParkingController : BaseApiController
    {
        private static readonly TimeSpan MaxBookingHorizon = TimeSpan.FromDays(30);
        private static readonly TimeSpan PastTolerance = TimeSpan.FromMinutes(5);

        private readonly IParkingService _parkingService;

        public ParkingController(IParkingService parkingService)
        {
            _parkingService = parkingService;
        }

        /// <summary>Active parking spaces with live availability. Public.</summary>
        [HttpGet]
        public async Task<ActionResult<List<ParkingSpaceDTO>>> GetParkingSpaces()
        {
            var parkingSpaces = await _parkingService.GetParkingSpacesAsync();
            return Ok(parkingSpaces);
        }

        [Authorize]
        [HttpPost("reserve")]
        public async Task<ActionResult> ReserveParkingSpace([FromBody] ReservationDTO dto)
        {
            var reservationTime = dto.ReservationTime.ToUtc();
            var now = DateTime.UtcNow;
            if (reservationTime < now - PastTolerance || reservationTime > now + MaxBookingHorizon)
            {
                ModelState.AddModelError(nameof(dto.ReservationTime),
                    "Reservation time must not be in the past and at most 30 days ahead.");
                return ValidationProblem(ModelState);
            }

            var userId = User.GetUserId().ToString();
            var (success, startTime, endTime) = await _parkingService.ReserveParkingSpaceAsync(
                userId, dto.ParkingSpaceId, reservationTime, dto.Duration);

            if (!success)
            {
                return BadRequest(new { message = "Failed to reserve parking space" });
            }

            return Ok(new { message = "Parking space reserved successfully", startTime, endTime });
        }

        [Authorize]
        [HttpGet("active-reservation")]
        public async Task<ActionResult<Reservation>> GetActiveReservation()
        {
            var reservation = await _parkingService.GetActiveReservationAsync(User.GetUserId().ToString());
            if (reservation == null)
            {
                return NotFound();
            }

            return Ok(reservation);
        }
    }
}

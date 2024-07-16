using System.Security.Claims;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingController : BaseApiController
    {
        // ParkingService for managing parking spaces and reservations
        private readonly IParkingService _parkingService;

        public ParkingController(IParkingService parkingService)
        {
            _parkingService = parkingService;
        }

        /// <summary>
        /// Gets all parking spaces.
        /// </summary>
        /// <returns>A list of parking spaces.</returns>
        [HttpGet]
        public async Task<ActionResult<List<ParkingSpace>>> GetParkingSpaces()
        {
            // Get all parking spaces and return them
            var parkingSpaces = await _parkingService.GetParkingSpacesAsync();
            return Ok(parkingSpaces);
        }

        /// <summary>
        /// Reserves a parking space.
        /// </summary>
        /// <param name="reservation">The reservation data.</param>
        /// <returns>A success message if the reservation was successful.</returns>
        [HttpPost("reserve")]
        public async Task<ActionResult> ReserveParkingSpace([FromBody] Reservation reservation)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authorized" });
            }

            reservation.ReservationTime = reservation.ReservationTime.ToUniversalTime();
            var (success, startTime, endTime) = await _parkingService.ReserveParkingSpaceAsync(userId, reservation.ParkingSpaceId, reservation.ReservationTime, reservation.Duration);

            if (!success)
            {
                return BadRequest(new { message = "Failed to reserve parking space" });
            }

            return Ok(new { message = "Parking space reserved successfully", startTime, endTime });
        }
        
        [HttpGet("active-reservation")]
        public async Task<ActionResult<Reservation>> GetActiveReservation()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var reservation = await _parkingService.GetActiveReservationAsync(userId);
            if (reservation == null)
            {
                return NotFound();
            }

            return Ok(reservation);
        }
    }
}

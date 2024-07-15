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
            // Get the user ID from the authenticated user's claim
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            // Convert the reservation time to UTC
            reservation.ReservationTime = reservation.ReservationTime.ToUniversalTime();
            // Reserve the parking space and return a success message if the reservation was successful
            var success = await _parkingService.ReserveParkingSpaceAsync(userId, reservation.ParkingSpaceId, reservation.ReservationTime, reservation.Duration);
            if (!success)
            {
                return BadRequest("Failed to reserve parking space");
            }

            return Ok("Parking space reserved successfully");
        }
    }
}

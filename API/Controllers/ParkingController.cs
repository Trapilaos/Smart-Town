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
        private readonly IParkingService _parkingService;

        public ParkingController(IParkingService parkingService)
        {
            _parkingService = parkingService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ParkingSpace>>> GetParkingSpaces()
        {
            var parkingSpaces = await _parkingService.GetParkingSpacesAsync();
            return Ok(parkingSpaces);
        }

        [HttpPost("reserve")]
        public async Task<ActionResult> ReserveParkingSpace([FromBody] Reservation reservation)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var success = await _parkingService.ReserveParkingSpaceAsync(userId, reservation.ParkingSpaceId, reservation.ReservationTime, reservation.Duration);
            if (!success)
            {
                return BadRequest("Failed to reserve parking space");
            }

            return Ok("Parking space reserved successfully");
        }
    }
}

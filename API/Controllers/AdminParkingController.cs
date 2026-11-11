using API.DTOs;
using API.Errors;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/parking")]
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminParkingController : BaseApiController
    {
        private readonly IParkingService _parking;
        private readonly IAuditService _audit;

        public AdminParkingController(IParkingService parking, IAuditService audit)
        {
            _parking = parking;
            _audit = audit;
        }

        /// <summary>Every parking space, including inactive ones.</summary>
        [HttpGet]
        public async Task<ActionResult<List<ParkingSpaceDTO>>> GetAll()
        {
            return Ok(await _parking.GetAllForAdminAsync());
        }

        [HttpPost]
        public async Task<ActionResult<ParkingSpaceDTO>> Create([FromBody] ParkingSpaceInputDTO input)
        {
            try
            {
                var created = await _parking.CreateAsync(input);
                await _audit.RecordAsync(ActorId, ActorName, "parking.create", "ParkingSpace", created.Id.ToString(),
                    $"{created.Name} ({created.Type}, {created.MaxVehicles} spaces)");
                return Ok(created);
            }
            catch (DomainValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ParkingSpaceDTO>> Update(int id, [FromBody] ParkingSpaceInputDTO input)
        {
            try
            {
                var updated = await _parking.UpdateAsync(id, input);
                if (updated == null) return NotFound();

                await _audit.RecordAsync(ActorId, ActorName, "parking.update", "ParkingSpace", id.ToString(),
                    $"{updated.Name} ({updated.Type}, {updated.MaxVehicles} spaces, active {updated.IsActive})");
                return Ok(updated);
            }
            catch (DomainValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Soft delete: the space is kept with IsActive = false and can no longer be reserved.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _parking.DeactivateAsync(id)) return NotFound();

            await _audit.RecordAsync(ActorId, ActorName, "parking.delete", "ParkingSpace", id.ToString(), "Space deactivated");
            return NoContent();
        }

        /// <summary>Reservations that have not ended yet. Usernames only.</summary>
        [HttpGet("{id:int}/reservations")]
        public async Task<ActionResult<List<ParkingReservationDTO>>> GetReservations(int id)
        {
            var reservations = await _parking.GetReservationsAsync(id);
            return reservations == null ? NotFound() : Ok(reservations);
        }
    }
}

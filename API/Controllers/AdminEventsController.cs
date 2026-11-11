using API.DTOs;
using API.Errors;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/events")]
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminEventsController : BaseApiController
    {
        private readonly IEventService _events;
        private readonly IAuditService _audit;

        public AdminEventsController(IEventService events, IAuditService audit)
        {
            _events = events;
            _audit = audit;
        }

        /// <summary>Every event, including unpublished ones.</summary>
        [HttpGet]
        public async Task<ActionResult<List<AdminEventDTO>>> GetAll()
        {
            return Ok(await _events.GetAllForAdminAsync());
        }

        [HttpPost]
        public async Task<ActionResult<AdminEventDTO>> Create([FromBody] EventInputDTO input)
        {
            var created = await _events.CreateAsync(input);
            await _audit.RecordAsync(ActorId, ActorName, "event.create", "Event", created.Id.ToString(),
                $"{created.Title} ({created.Category}, published {created.IsPublished})");
            return Ok(created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<AdminEventDTO>> Update(int id, [FromBody] EventInputDTO input)
        {
            var updated = await _events.UpdateAsync(id, input);
            if (updated == null) return NotFound();

            await _audit.RecordAsync(ActorId, ActorName, "event.update", "Event", id.ToString(),
                $"{updated.Title} ({updated.Category}, published {updated.IsPublished})");
            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _events.DeleteAsync(id)) return NotFound();

            await _audit.RecordAsync(ActorId, ActorName, "event.delete", "Event", id.ToString(), "Event deleted");
            return NoContent();
        }
    }
}

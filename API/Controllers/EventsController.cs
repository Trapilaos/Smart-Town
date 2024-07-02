using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> CreateEvent([FromBody] Event newEvent)
        {
            var createdEvent = await _eventService.CreateEventAsync(newEvent);
            return Ok(createdEvent);
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _eventService.GetEventsAsync();
            return Ok(events);
        }

        [HttpPost("{eventId}/interest")]
        [Authorize(Policy = "RequireMemberRole")]
        public async Task<IActionResult> DeclareInterest(int eventId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var eventItem = await _eventService.DeclareInterestAsync(eventId, userId);
            if (eventItem == null) return NotFound();

            return Ok(eventItem);
        }
    }
}
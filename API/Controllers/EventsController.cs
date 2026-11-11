using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class EventsController : BaseApiController
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDTO dto)
        {
            var createdEvent = await _eventService.CreateEventAsync(new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                PhotoUrl = dto.PhotoUrl,
                Date = dto.Date.ToUtc()
            });
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
            var userId = User.GetUserId().ToString();
            var eventItem = await _eventService.DeclareInterestAsync(eventId, userId);
            if (eventItem == null)
                return NotFound();

            return Ok(eventItem);
        }

        [HttpDelete("{eventId}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            var result = await _eventService.DeleteEventAsync(eventId);
            if (!result)
                return NotFound();

            return Ok();
        }
    }
}

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

        /// <summary>Published events. Public. Signed-in callers also get isInterested.</summary>
        /// <param name="fromDate">Only events starting on or after this UTC instant.</param>
        /// <param name="category">Culture, Sports, Council, Environment or Other.</param>
        /// <param name="includePast">When false (the default), events that have ended are hidden.</param>
        [HttpGet]
        public async Task<ActionResult<List<EventDTO>>> GetEvents(
            [FromQuery(Name = "from")] DateTime? fromDate,
            [FromQuery] string category,
            [FromQuery] bool includePast = false)
        {
            EventCategory? parsed = null;
            if (!string.IsNullOrWhiteSpace(category))
            {
                if (!Enum.TryParse<EventCategory>(category.Trim(), ignoreCase: true, out var value) || !Enum.IsDefined(value))
                    return BadRequest("Unknown event category");
                parsed = value;
            }

            var events = await _eventService.GetPublishedAsync(fromDate?.ToUtc(), parsed, includePast, ViewerId);
            return Ok(events);
        }

        [HttpGet("{eventId:int}")]
        public async Task<ActionResult<EventDTO>> GetEvent(int eventId)
        {
            var ev = await _eventService.GetPublishedByIdAsync(eventId, ViewerId);
            return ev == null ? NotFound() : Ok(ev);
        }

        [HttpPost("{eventId:int}/interest")]
        [Authorize(Policy = "RequireMemberRole")]
        public async Task<ActionResult<EventDTO>> DeclareInterest(int eventId)
        {
            var ev = await _eventService.SetInterestAsync(eventId, ActorId, interested: true);
            return ev == null ? NotFound() : Ok(ev);
        }

        [HttpDelete("{eventId:int}/interest")]
        [Authorize(Policy = "RequireMemberRole")]
        public async Task<ActionResult<EventDTO>> WithdrawInterest(int eventId)
        {
            var ev = await _eventService.SetInterestAsync(eventId, ActorId, interested: false);
            return ev == null ? NotFound() : Ok(ev);
        }

        private string ViewerId => User.Identity?.IsAuthenticated == true ? ActorId : null;
    }
}

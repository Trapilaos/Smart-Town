using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : BaseApiController
    {
        // EventService for managing events
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// Creates a new event.
        /// </summary>
        /// <param name="newEvent">The event data.</param>
        /// <returns>The created event.</returns>
        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> CreateEvent([FromBody] Event newEvent)
        {
            // Create the event and return the created event
            var createdEvent = await _eventService.CreateEventAsync(newEvent);
            return Ok(createdEvent);
        }

        /// <summary>
        /// Gets all events.
        /// </summary>
        /// <returns>A list of events.</returns>
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            // Get all events and return them
            var events = await _eventService.GetEventsAsync();
            return Ok(events);
        }

        /// <summary>
        /// Declares a user's interest in an event.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <returns>The updated event with the interested user.</returns>
        [HttpPost("{eventId}/interest")]
        [Authorize(Policy = "RequireMemberRole")]
        public async Task<IActionResult> DeclareInterest(int eventId)
        {
            // Get the user ID from the authenticated user's claim
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            // Declare the user's interest in the event and return the updated event
            var eventItem = await _eventService.DeclareInterestAsync(eventId, userId);
            if (eventItem == null)
                return NotFound();

            return Ok(eventItem);
        }

        /// <summary>
        /// Deletes an event.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <returns>A 200 OK response if the event was found and deleted.</returns>
        [HttpDelete("{eventId}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            // Delete the event and return a 200 OK response if the event was found and deleted
            var result = await _eventService.DeleteEventAsync(eventId);
            if (!result)
                return NotFound();

            return Ok();
        }
    }
}

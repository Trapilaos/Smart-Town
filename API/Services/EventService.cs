using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class EventService : IEventService
    {
        private readonly DataContext _context;

        public EventService(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new event in the database.
        /// </summary>
        /// <param name="newEvent">The event object to be created.</param>
        /// <returns>The newly created event object.</returns>
        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return newEvent;
        }

        /// <summary>
        /// Retrieves all events from the database.
        /// </summary>
        /// <returns>A list of event objects.</returns>
        public async Task<List<Event>> GetEventsAsync()
        {
            return await _context.Events.ToListAsync();
        }

        /// <summary>
        /// Declares a user's interest in an event.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The updated event object, or null if the event doesn't exist.</returns>
        public async Task<Event> DeclareInterestAsync(int eventId, string userId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return null;

            if (ev.InterestedUsers.Contains(userId)) return ev;

            ev.InterestedUsers.Add(userId);
            await _context.SaveChangesAsync();

            return ev;
        }

        /// <summary>
        /// Deletes an event from the database.
        /// </summary>
        /// <param name="eventId">The ID of the event to be deleted.</param>
        /// <returns>True if the event was deleted successfully, false otherwise.</returns>
        public async Task<bool> DeleteEventAsync(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return false;

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

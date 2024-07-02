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

        public async Task<Event> CreateEventAsync(Event newEvent)
        {
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return newEvent;
        }

        public async Task<IEnumerable<Event>> GetEventsAsync()
        {
            return await _context.Events.ToListAsync();
        }

        public async Task<Event> DeclareInterestAsync(int eventId, string userId)
        {
            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null) return null;

            eventItem.InterestedUsers.Add(userId);
            await _context.SaveChangesAsync();
            return eventItem;
        }
    }
}
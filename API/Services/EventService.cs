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

        public async Task<List<Event>> GetEventsAsync()
        {
            return await _context.Events.ToListAsync();
        }

        public async Task<Event> DeclareInterestAsync(int eventId, string userId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return null;

            if (ev.InterestedUsers.Contains(userId)) return ev;

            ev.InterestedUsers.Add(userId);
            await _context.SaveChangesAsync();

            return ev;
        }

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

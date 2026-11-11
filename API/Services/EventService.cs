using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
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

        public async Task<List<EventDTO>> GetPublishedAsync(DateTime? from, EventCategory? category, bool includePast, string viewerUserId)
        {
            var query = _context.Events.AsNoTracking().Where(e => e.IsPublished);

            if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
            if (category.HasValue) query = query.Where(e => e.Category == category.Value);
            if (!includePast)
            {
                // An event with an end date stays listed until it ends.
                var now = DateTime.UtcNow;
                query = query.Where(e => e.Date >= now || (e.EndDate != null && e.EndDate >= now));
            }

            var events = await query.OrderBy(e => e.Date).ThenBy(e => e.Id).ToListAsync();
            return await BuildDtosAsync(events, viewerUserId);
        }

        public async Task<EventDTO> GetPublishedByIdAsync(int id, string viewerUserId)
        {
            var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.IsPublished);
            if (ev == null) return null;

            return (await BuildDtosAsync(new List<Event> { ev }, viewerUserId)).Single();
        }

        public async Task<EventDTO> SetInterestAsync(int eventId, string userId, bool interested)
        {
            var published = await _context.Events.AsNoTracking().AnyAsync(e => e.Id == eventId && e.IsPublished);
            if (!published) return null;

            var existing = await _context.EventInterests.FirstOrDefaultAsync(i => i.EventId == eventId && i.UserId == userId);
            if (interested && existing == null)
            {
                _context.EventInterests.Add(new EventInterest { EventId = eventId, UserId = userId, CreatedAt = DateTime.UtcNow });
            }
            else if (!interested && existing != null)
            {
                _context.EventInterests.Remove(existing);
            }

            await _context.SaveChangesAsync();
            return await GetPublishedByIdAsync(eventId, userId);
        }

        public async Task<List<AdminEventDTO>> GetAllForAdminAsync()
        {
            var events = await _context.Events.AsNoTracking().OrderByDescending(e => e.Date).ToListAsync();
            var counts = await InterestCountsAsync(events.Select(e => e.Id).ToList());
            return events.Select(e => AdminEventDTO.From(e, counts.GetValueOrDefault(e.Id))).ToList();
        }

        public async Task<AdminEventDTO> CreateAsync(EventInputDTO input)
        {
            var ev = new Event { CreatedAt = DateTime.UtcNow };
            Apply(ev, input);
            _context.Events.Add(ev);
            await _context.SaveChangesAsync();

            return AdminEventDTO.From(ev, 0);
        }

        public async Task<AdminEventDTO> UpdateAsync(int id, EventInputDTO input)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return null;

            Apply(ev, input);
            await _context.SaveChangesAsync();

            var counts = await InterestCountsAsync(new List<int> { id });
            return AdminEventDTO.From(ev, counts.GetValueOrDefault(id));
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return false;

            await _context.EventInterests.Where(i => i.EventId == id).ExecuteDeleteAsync();
            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return true;
        }

        private static void Apply(Event ev, EventInputDTO input)
        {
            ev.Title = input.Title.Trim();
            ev.Description = Clean(input.Description);
            ev.PhotoUrl = Clean(input.PhotoUrl);
            ev.Location = Clean(input.Location);
            ev.Latitude = input.Lat;
            ev.Longitude = input.Lng;
            ev.Date = input.Date.ToUtc();
            ev.EndDate = input.EndDate?.ToUtc();
            ev.Category = Enum.Parse<EventCategory>(input.Category);
            ev.IsPublished = input.IsPublished;
        }

        private static string Clean(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task<Dictionary<int, int>> InterestCountsAsync(List<int> eventIds)
        {
            return await _context.EventInterests.AsNoTracking()
                .Where(i => eventIds.Contains(i.EventId))
                .GroupBy(i => i.EventId)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventId, x => x.Count);
        }

        private async Task<List<EventDTO>> BuildDtosAsync(List<Event> events, string viewerUserId)
        {
            var ids = events.Select(e => e.Id).ToList();
            var counts = await InterestCountsAsync(ids);

            var mine = viewerUserId == null
                ? new HashSet<int>()
                : (await _context.EventInterests.AsNoTracking()
                    .Where(i => i.UserId == viewerUserId && ids.Contains(i.EventId))
                    .Select(i => i.EventId)
                    .ToListAsync()).ToHashSet();

            return events.Select(e => EventDTO.From(e, counts.GetValueOrDefault(e.Id), mine.Contains(e.Id))).ToList();
        }
    }
}

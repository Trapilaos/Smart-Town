using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Seeds
{
    /// <summary>Sample municipal events for Glyfada. Seeds only when the table is empty.</summary>
    public static class EventSeed
    {
        public static async Task SeedEventsAsync(DataContext context)
        {
            if (await context.Events.AnyAsync()) return;

            await context.Events.AddRangeAsync(Generate(DateTime.UtcNow));
            await context.SaveChangesAsync();
        }

        public static List<Event> Generate(DateTime nowUtc)
        {
            var today = nowUtc.Date;

            Event Create(string title, string description, EventCategory category, string location,
                double lat, double lng, DateTime start, DateTime? end, bool published) => new()
            {
                Title = title,
                Description = description,
                Category = category,
                Location = location,
                Latitude = lat,
                Longitude = lng,
                Date = start,
                EndDate = end,
                IsPublished = published,
                CreatedAt = nowUtc,
            };

            return new List<Event>
            {
                Create("Neighbourhood recycling day", "Drop off paper, glass and batteries at the central square. Staff will be on site.",
                    EventCategory.Environment, "Central square, Glyfada", 37.8660, 23.7540,
                    today.AddDays(7).AddHours(9), today.AddDays(7).AddHours(14), published: true),
                Create("Open session of the municipal council", "Residents may address the council on local matters.",
                    EventCategory.Council, "Town hall, Glyfada", 37.8650, 23.7530,
                    today.AddDays(14).AddHours(19), null, published: true),
                Create("Community 5K run", "A short family-friendly run through the seafront and the park.",
                    EventCategory.Sports, "Glyfada seafront", 37.8610, 23.7570,
                    today.AddDays(35).AddHours(8), null, published: true),
                Create("Summer music evenings", "Live local bands every Friday in the amphitheatre.",
                    EventCategory.Culture, "Amphitheatre, Glyfada", 37.8690, 23.7600,
                    today.AddDays(20).AddHours(20), today.AddDays(20).AddHours(23), published: true),
                Create("Winter lights ceremony", "The annual tree lighting in the square.",
                    EventCategory.Culture, "Central square, Glyfada", 37.8660, 23.7540,
                    today.AddDays(-30).AddHours(18), null, published: true),
                Create("Draft: autumn fair", "Not yet published. Only admins can see this entry.",
                    EventCategory.Other, "Central square, Glyfada", 37.8660, 23.7540,
                    today.AddDays(60).AddHours(10), null, published: false),
            };
        }
    }
}

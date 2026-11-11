namespace API.Entities
{
    public enum EventCategory
    {
        Culture,
        Sports,
        Council,
        Environment,
        Other,
    }

    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public string Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime Date { get; set; }
        public DateTime? EndDate { get; set; }
        /// <summary>Stored as text.</summary>
        public EventCategory Category { get; set; } = EventCategory.Other;
        /// <summary>Unpublished events are hidden from every public endpoint.</summary>
        public bool IsPublished { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>A member's "interested" mark. One row per (event, user).</summary>
    public class EventInterest
    {
        public int EventId { get; set; }
        public Event Event { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using API.Entities;

namespace API.Interfaces
{
    public interface IEventService
    {
        Task<Event> CreateEventAsync(Event newEvent);
        Task<IEnumerable<Event>> GetEventsAsync();
        Task<Event> DeclareInterestAsync(int eventId, string userId);
    }
}
using API.Entities;

namespace API.Interfaces
{
    public interface IEventService
    {
        Task<Event> CreateEventAsync(Event newEvent);
        Task<List<Event>> GetEventsAsync();
        Task<Event> DeclareInterestAsync(int eventId, string userId);
        Task<bool> DeleteEventAsync(int eventId);
    }
}

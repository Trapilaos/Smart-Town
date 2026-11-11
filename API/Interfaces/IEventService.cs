using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IEventService
    {
        /// <summary>Published events only. Ordered by start date.</summary>
        Task<List<EventDTO>> GetPublishedAsync(DateTime? from, EventCategory? category, bool includePast, string viewerUserId);
        /// <summary>Returns null when the event does not exist or is not published.</summary>
        Task<EventDTO> GetPublishedByIdAsync(int id, string viewerUserId);
        /// <summary>Idempotent. Returns null when the event is not published.</summary>
        Task<EventDTO> SetInterestAsync(int eventId, string userId, bool interested);

        Task<List<AdminEventDTO>> GetAllForAdminAsync();
        Task<AdminEventDTO> CreateAsync(EventInputDTO input);
        /// <summary>Returns null when the event does not exist.</summary>
        Task<AdminEventDTO> UpdateAsync(int id, EventInputDTO input);
        /// <summary>Returns false when the event does not exist.</summary>
        Task<bool> DeleteAsync(int id);
    }
}

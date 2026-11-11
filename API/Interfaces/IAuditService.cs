using API.DTOs;

namespace API.Interfaces
{
    public interface IAuditService
    {
        /// <summary>Appends one audit row and saves it. Details are truncated to 500 characters.</summary>
        Task RecordAsync(string userId, string username, string action, string entityType, string entityId, string details);

        /// <summary>Most recent entries first.</summary>
        Task<List<AuditEntryDTO>> GetRecentAsync(int take);
    }
}

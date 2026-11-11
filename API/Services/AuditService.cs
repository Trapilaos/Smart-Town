using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class AuditService : IAuditService
    {
        public const int MaxDetailsLength = 500;

        private readonly DataContext _context;

        public AuditService(DataContext context)
        {
            _context = context;
        }

        public async Task RecordAsync(string userId, string username, string action, string entityType, string entityId, string details)
        {
            var text = details ?? string.Empty;
            if (text.Length > MaxDetailsLength) text = text[..MaxDetailsLength];

            _context.AuditEntries.Add(new AuditEntry
            {
                UserId = userId,
                Username = username,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                AtUtc = DateTime.UtcNow,
                Details = text,
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditEntryDTO>> GetRecentAsync(int take)
        {
            var entries = await _context.AuditEntries
                .AsNoTracking()
                .OrderByDescending(a => a.Id)
                .Take(take)
                .ToListAsync();

            return entries.Select(a => new AuditEntryDTO
            {
                Id = a.Id,
                Username = a.Username,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                AtUtc = a.AtUtc,
                Details = a.Details,
            }).ToList();
        }
    }
}

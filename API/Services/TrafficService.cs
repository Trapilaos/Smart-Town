using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class TrafficService : ITrafficService
    {
        private readonly DataContext _context;

        public TrafficService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<TrafficData>> GetCurrentTrafficDataAsync()
        {
            var now = DateTime.Now;
            return await _context.TrafficData
                .Where(td => td.Timestamp.Hour == now.Hour && td.Timestamp.Date == now.Date)
                .ToListAsync();
        }

        public async Task<TrafficData> GetMostCrowdedStreetAsync()
        {
            return await _context.TrafficData
                .OrderByDescending(td => td.TrafficFlow)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateTrafficDataAsync()
        {
            var newTrafficData = TrafficDataGenerator.GenerateTrafficData();
            await ClearOldTrafficDataAsync();
            await _context.TrafficData.AddRangeAsync(newTrafficData);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TrafficData>> GetTrafficDataAsync()
        {
            return await _context.TrafficData.ToListAsync();
        }

        private async Task ClearOldTrafficDataAsync()
        {
            var oldData = _context.TrafficData
                .Where(td => td.Timestamp < DateTime.Now.AddDays(-1)); // Keep only the last day's data

            _context.TrafficData.RemoveRange(oldData);
            await _context.SaveChangesAsync();
        }
    }
}

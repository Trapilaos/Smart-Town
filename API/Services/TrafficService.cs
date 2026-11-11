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

        /// <summary>
        /// Gets the current traffic data for the current hour.
        /// </summary>
        /// <returns>A list of traffic data for the current hour.</returns>
        public async Task<List<TrafficData>> GetCurrentTrafficDataAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.TrafficData
                .Where(td => td.Timestamp.Hour == now.Hour && td.Timestamp.Date == now.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Gets the traffic data for the street with the highest traffic flow.
        /// </summary>
        /// <returns>The traffic data for the most crowded street.</returns>
        public async Task<TrafficData> GetMostCrowdedStreetAsync()
        {
            return await _context.TrafficData
                .OrderByDescending(td => td.TrafficFlow)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Updates the traffic data by generating new data and clearing old data.
        /// </summary>
        public async Task UpdateTrafficDataAsync()
        {
            var newTrafficData = TrafficDataGenerator.GenerateTrafficData();
            await ClearOldTrafficDataAsync();
            await _context.TrafficData.AddRangeAsync(newTrafficData);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves all traffic data from the database.
        /// </summary>
        /// <returns>A list of all traffic data.</returns>
        public async Task<List<TrafficData>> GetTrafficDataAsync()
        {
            return await _context.TrafficData.ToListAsync();
        }

        /// <summary>
        /// Clears all existing traffic data before a fresh set is generated.
        /// </summary>
        private async Task ClearOldTrafficDataAsync()
        {
            var oldData = _context.TrafficData
                .ToList(); // the generator produces a fresh full 24h set, so replace everything

            _context.TrafficData.RemoveRange(oldData);
            await _context.SaveChangesAsync();
        }
    }
}

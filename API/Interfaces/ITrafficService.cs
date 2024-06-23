using API.Entities;

namespace API.Interfaces
{
    public interface ITrafficService
    {
        Task<List<TrafficData>> GetCurrentTrafficDataAsync();
        Task<TrafficData> GetMostCrowdedStreetAsync();
        Task UpdateTrafficDataAsync();
        Task<List<TrafficData>> GetTrafficDataAsync();
    }
}

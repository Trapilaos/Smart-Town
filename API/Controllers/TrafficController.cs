using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrafficController : BaseApiController
    {
        // TrafficService for managing traffic data
        private readonly ITrafficService _trafficService;

        public TrafficController(ITrafficService trafficService)
        {
            _trafficService = trafficService;
        }

        /// <summary>
        /// Gets the current traffic data.
        /// </summary>
        /// <returns>A list of traffic data.</returns>
        [HttpGet("current")]
        public async Task<ActionResult<List<TrafficData>>> GetCurrentTrafficData()
        {
            // Get the current traffic data and return it
            var trafficData = await _trafficService.GetCurrentTrafficDataAsync();
            return Ok(trafficData);
        }

        /// <summary>
        /// Gets the most crowded street.
        /// </summary>
        /// <returns>The most crowded street's traffic data.</returns>
        [HttpGet("most-crowded")]
        public async Task<ActionResult<TrafficData>> GetMostCrowdedStreet()
        {
            // Get the most crowded street's traffic data and return it
            var mostCrowdedStreet = await _trafficService.GetMostCrowdedStreetAsync();
            return Ok(mostCrowdedStreet);
        }

        /// <summary>
        /// Updates the traffic data.
        /// </summary>
        /// <returns>A 204 No Content response if the update was successful.</returns>
        [HttpPost("update")]
        public async Task<ActionResult> UpdateTrafficData()
        {
            // Update the traffic data and return a 204 No Content response if the update was successful
            await _trafficService.UpdateTrafficDataAsync();
            return NoContent();
        }
    }
}

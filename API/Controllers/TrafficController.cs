using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrafficController : BaseApiController
    {
        private readonly ITrafficService _trafficService;

        public TrafficController(ITrafficService trafficService)
        {
            _trafficService = trafficService;
        }

        [HttpGet("current")]
        public async Task<ActionResult<List<TrafficData>>> GetCurrentTrafficData()
        {
            var trafficData = await _trafficService.GetCurrentTrafficDataAsync();
            return Ok(trafficData);
        }

        [HttpGet("most-crowded")]
        public async Task<ActionResult<TrafficData>> GetMostCrowdedStreet()
        {
            var mostCrowdedStreet = await _trafficService.GetMostCrowdedStreetAsync();
            return Ok(mostCrowdedStreet);
        }

        [HttpPost("update")]
        public async Task<ActionResult> UpdateTrafficData()
        {
            await _trafficService.UpdateTrafficDataAsync();
            return NoContent();
        }
    }
}

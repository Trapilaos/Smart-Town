using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LightingController : BaseApiController
    {
        // SmartLightingService for managing lighting status
        private readonly SmartLightingService _smartLightingService;

        public LightingController(SmartLightingService smartLightingService)
        {
            _smartLightingService = smartLightingService;
        }

        /// <summary>
        /// Gets the lighting status for a town.
        /// </summary>
        /// <param name="town">The name of the town.</param>
        /// <returns>The lighting status for the town.</returns>
        [HttpGet("status")]
        public async Task<IActionResult> GetLightingStatus(string town)
        {
            // Get the lighting status for the town and return it
            var lightingStatus = await _smartLightingService.GetLightingStatusAsync(town);
            return Ok(lightingStatus);
        }
    }
}

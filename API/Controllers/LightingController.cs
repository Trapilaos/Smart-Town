using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LightingController : BaseApiController
    {
        private readonly SmartLightingService _smartLightingService;

        public LightingController(SmartLightingService smartLightingService)
        {
            _smartLightingService = smartLightingService;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetLightingStatus(string town)
        {
            var (status, brightness) = await _smartLightingService.GetLightingStatusAsync(town);
            return Ok(new { status, brightness });
        }
    }
}

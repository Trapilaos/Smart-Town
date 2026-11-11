using API.DTOs;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/waste")]
    public class WasteController : BaseApiController
    {
        private readonly IWasteService _wasteService;

        public WasteController(IWasteService wasteService)
        {
            _wasteService = wasteService;
        }

        /// <summary>Lists active waste bins with their fill percentage. Public.</summary>
        /// <param name="type">Optional filter: General, Recycling, Glass, Organic or Paper.</param>
        [HttpGet("bins")]
        public async Task<ActionResult<List<WasteBinDTO>>> GetBins([FromQuery] string type = null)
        {
            if (!WasteService.TryParseType(type, out var parsed))
                return BadRequest("Unknown bin type");

            return Ok(await _wasteService.GetActiveBinsAsync(parsed));
        }
    }
}

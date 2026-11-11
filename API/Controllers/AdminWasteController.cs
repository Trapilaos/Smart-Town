using API.DTOs;
using API.Errors;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/waste")]
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminWasteController : BaseApiController
    {
        private readonly IWasteService _wasteService;

        public AdminWasteController(IWasteService wasteService)
        {
            _wasteService = wasteService;
        }

        [HttpPost("bins")]
        public async Task<ActionResult<WasteBinDTO>> CreateBin([FromBody] WasteBinInputDTO input)
        {
            try
            {
                return Ok(await _wasteService.CreateBinAsync(input));
            }
            catch (DomainValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("bins/{id:int}")]
        public async Task<ActionResult<WasteBinDTO>> UpdateBin(int id, [FromBody] WasteBinInputDTO input)
        {
            try
            {
                var updated = await _wasteService.UpdateBinAsync(id, input);
                return updated == null ? NotFound() : Ok(updated);
            }
            catch (DomainValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Soft delete: the bin is kept with IsActive = false.</summary>
        [HttpDelete("bins/{id:int}")]
        public async Task<IActionResult> DeleteBin(int id)
        {
            return await _wasteService.DeactivateBinAsync(id) ? NoContent() : NotFound();
        }

        [HttpPut("bins/{id:int}/empty")]
        public async Task<ActionResult<WasteBinDTO>> MarkEmptied(int id)
        {
            var emptied = await _wasteService.MarkEmptiedAsync(id);
            return emptied == null ? NotFound() : Ok(emptied);
        }

        /// <summary>Collection route: nearest-neighbour refined with 2-opt, starting and ending at the municipality centre.</summary>
        [HttpGet("route")]
        public async Task<ActionResult<WasteRouteDTO>> GetRoute(
            [FromQuery] string type = null,
            [FromQuery][Range(0, 100)] int minFillPercent = 60)
        {
            if (!WasteService.TryParseType(type, out var parsed))
                return BadRequest("Unknown bin type");

            return Ok(await _wasteService.BuildRouteAsync(parsed, minFillPercent));
        }
    }
}

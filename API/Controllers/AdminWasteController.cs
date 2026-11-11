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
        private readonly IAuditService _audit;

        public AdminWasteController(IWasteService wasteService, IAuditService audit)
        {
            _wasteService = wasteService;
            _audit = audit;
        }

        [HttpPost("bins")]
        public async Task<ActionResult<WasteBinDTO>> CreateBin([FromBody] WasteBinInputDTO input)
        {
            try
            {
                var created = await _wasteService.CreateBinAsync(input);
                await _audit.RecordAsync(ActorId, ActorName, "waste.bin.create", "WasteBin", created.Id.ToString(),
                    $"{created.Type} bin at {created.Location}");
                return Ok(created);
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
                if (updated == null) return NotFound();

                await _audit.RecordAsync(ActorId, ActorName, "waste.bin.update", "WasteBin", id.ToString(),
                    $"{updated.Type} bin at {updated.Location}, fill {updated.FillPercent}%");
                return Ok(updated);
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
            if (!await _wasteService.DeactivateBinAsync(id)) return NotFound();

            await _audit.RecordAsync(ActorId, ActorName, "waste.bin.delete", "WasteBin", id.ToString(), "Bin deactivated");
            return NoContent();
        }

        [HttpPut("bins/{id:int}/empty")]
        public async Task<ActionResult<WasteBinDTO>> MarkEmptied(int id)
        {
            var emptied = await _wasteService.MarkEmptiedAsync(id);
            if (emptied == null) return NotFound();

            await _audit.RecordAsync(ActorId, ActorName, "waste.bin.empty", "WasteBin", id.ToString(), "Bin emptied");
            return Ok(emptied);
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

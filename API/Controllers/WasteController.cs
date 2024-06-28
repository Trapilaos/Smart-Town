using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WasteController : BaseApiController
    {
        private readonly IWasteManagementService _wasteManagementService;

        public WasteController(IWasteManagementService wasteManagementService)
        {
            _wasteManagementService = wasteManagementService;
        }

        [HttpGet]
        public async Task<ActionResult<List<WasteBinDTO>>> GetWasteBins()
        {
            return await _wasteManagementService.GetWasteBinsAsync();
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<WasteBinDTO>> UpdateWasteBin(int id, WasteBinDTO wasteBinDto)
        {
            if (id != wasteBinDto.Id) return BadRequest("Bin ID mismatch");

            var updatedWasteBin = await _wasteManagementService.UpdateWasteBinAsync(wasteBinDto);
            if (updatedWasteBin == null) return NotFound();
            return Ok(updatedWasteBin);
        }

        [HttpGet("optimalpath")]
        public async Task<ActionResult<List<string>>> GetOptimalPath()
        {
            var optimalPath = await _wasteManagementService.GetOptimalPathAsync();
            return Ok(optimalPath);
        }
    }
}

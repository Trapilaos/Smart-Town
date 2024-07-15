using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WasteController : BaseApiController
    {
        // WasteManagementService for managing waste bins and optimal paths
        private readonly IWasteManagementService _wasteManagementService;

        public WasteController(IWasteManagementService wasteManagementService)
        {
            _wasteManagementService = wasteManagementService;
        }

        /// <summary>
        /// Gets all waste bins.
        /// </summary>
        /// <returns>A list of waste bins.</returns>
        [HttpGet]
        public async Task<ActionResult<List<WasteBinDTO>>> GetWasteBins()
        {
            // Get all waste bins and return them
            return await _wasteManagementService.GetWasteBinsAsync();
        }

        /// <summary>
        /// Updates a waste bin.
        /// </summary>
        /// <param name="id">The waste bin ID.</param>
        /// <param name="wasteBinDto">The updated waste bin data.</param>
        /// <returns>The updated waste bin.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<WasteBinDTO>> UpdateWasteBin(int id, WasteBinDTO wasteBinDto)
        {
            // Check if the ID in the URL matches the ID in the request body
            if (id != wasteBinDto.Id)
                return BadRequest("Bin ID mismatch");

            // Update the waste bin and return a 404 Not Found response if the waste bin is not found
            var updatedWasteBin = await _wasteManagementService.UpdateWasteBinAsync(wasteBinDto);
            if (updatedWasteBin == null)
                return NotFound();

            return Ok(updatedWasteBin);
        }

        /// <summary>
        /// Gets the optimal path for waste collection.
        /// </summary>
        /// <returns>The optimal path or a message if no path is needed.</returns>
        [HttpGet("optimalpath")]
        public async Task<ActionResult> GetOptimalPath()
        {
            // Get the optimal path and check if it's needed
            var (needsPath, optimalPath) = await _wasteManagementService.GetOptimalPathAsync();
            if (!needsPath)
            {
                return Ok(new { message = "No need for optimal path, all bins are empty" });
            }

            // Return the optimal path
            return Ok(optimalPath);
        }
    }
}

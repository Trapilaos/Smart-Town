using API.DTOs;
using API.Extensions;
using API.Geo;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/municipality")]
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminMunicipalityController : BaseApiController
    {
        private readonly IMunicipalityService _municipalities;
        private readonly BoundaryLookupService _lookup;

        public AdminMunicipalityController(IMunicipalityService municipalities, BoundaryLookupService lookup)
        {
            _municipalities = municipalities;
            _lookup = lookup;
        }

        [HttpPut]
        public async Task<ActionResult<MunicipalityDTO>> Update([FromBody] MunicipalityUpdateDTO dto)
        {
            try
            {
                return Ok(await _municipalities.UpdateAsync(dto));
            }
            catch (GeoJsonException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Searches OpenStreetMap for administrative boundaries. Admin only, rate limited.</summary>
        [HttpPost("boundary/search")]
        [EnableRateLimiting(RateLimitPolicies.BoundarySearch)]
        public async Task<ActionResult<List<BoundaryCandidateDTO>>> SearchBoundary([FromBody] BoundarySearchDTO dto)
        {
            try
            {
                var candidates = await _lookup.SearchAsync(dto.Query);
                return Ok(candidates.Select(ToDto).ToList());
            }
            catch (BoundaryLookupException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, "Boundary lookup is unavailable, try again later");
            }
        }

        /// <summary>Fetches the chosen OpenStreetMap object again, validates it and stores it as the boundary.</summary>
        [HttpPost("boundary/apply")]
        public async Task<ActionResult<MunicipalityDTO>> ApplyBoundary([FromBody] BoundaryApplyDTO dto)
        {
            BoundaryCandidate candidate;
            try
            {
                candidate = await _lookup.LookupAsync(dto.OsmType, dto.OsmId);
            }
            catch (BoundaryLookupException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, "Boundary lookup is unavailable, try again later");
            }

            if (candidate == null)
                return BadRequest("That OpenStreetMap object is not an administrative boundary polygon");

            await _municipalities.ApplyBoundaryAsync(candidate);
            return Ok(await _municipalities.GetAsync());
        }

        private static BoundaryCandidateDTO ToDto(BoundaryCandidate candidate) => new(
            candidate.OsmType,
            candidate.OsmId,
            candidate.DisplayName,
            candidate.CenterLat,
            candidate.CenterLng,
            GeoService.ToJsonElement(candidate.Geometry));
    }
}

using System.ComponentModel.DataAnnotations;
using API.DTOs;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/lighting")]
    public class LightingController : BaseApiController
    {
        private readonly IMunicipalityService _municipalities;

        public LightingController(IMunicipalityService municipalities)
        {
            _municipalities = municipalities;
        }

        /// <summary>Lighting schedule from today (local time) for 1 to 31 days. Public.</summary>
        [HttpGet("schedule")]
        public async Task<ActionResult<List<LightingDayDTO>>> GetSchedule([FromQuery][Range(1, 31)] int days = 7)
        {
            var site = LightingScheduleService.SiteFor(await _municipalities.GetEntityAsync());
            var today = LightingScheduleService.LocalDate(site, DateTime.UtcNow);

            var rows = LightingScheduleService.GetSchedule(site, today, days);
            return Ok(rows.Select(LightingDayDTO.From).ToList());
        }

        /// <summary>The current lighting status, brightness and next change. Public.</summary>
        [HttpGet("now")]
        public async Task<ActionResult<LightingNowDTO>> GetNow()
        {
            var site = LightingScheduleService.SiteFor(await _municipalities.GetEntityAsync());
            var now = LightingScheduleService.GetNow(site, DateTime.UtcNow);
            // Local offset, like the schedule rows.

            return Ok(new LightingNowDTO
            {
                Status = now.Status,
                BrightnessPercent = now.BrightnessPercent,
                NextChange = now.NextChange == null ? null : new LightingNextChangeDTO
                {
                    At = TimeZoneInfo.ConvertTime(
                        new DateTimeOffset(DateTime.SpecifyKind(now.NextChange.AtUtc, DateTimeKind.Utc)),
                        site.TimeZone),
                    ToStatus = now.NextChange.ToStatus,
                },
            });
        }
    }
}

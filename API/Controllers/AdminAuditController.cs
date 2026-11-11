using System.ComponentModel.DataAnnotations;
using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/audit")]
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminAuditController : BaseApiController
    {
        private readonly IAuditService _audit;

        public AdminAuditController(IAuditService audit)
        {
            _audit = audit;
        }

        /// <summary>The latest admin actions, newest first.</summary>
        [HttpGet]
        public async Task<ActionResult<List<AuditEntryDTO>>> GetRecent([FromQuery][Range(1, 200)] int take = 50)
        {
            return Ok(await _audit.GetRecentAsync(take));
        }
    }
}

using API.DTOs;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/municipality")]
    public class MunicipalityController : BaseApiController
    {
        private readonly IMunicipalityService _municipalities;

        public MunicipalityController(IMunicipalityService municipalities)
        {
            _municipalities = municipalities;
        }

        /// <summary>Public municipality name, centre, zoom and boundary.</summary>
        [HttpGet]
        public async Task<ActionResult<MunicipalityDTO>> Get()
        {
            return Ok(await _municipalities.GetAsync());
        }
    }
}

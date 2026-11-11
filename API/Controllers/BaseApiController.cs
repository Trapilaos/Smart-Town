using API.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {
        /// <summary>The signed-in user's id, or null for anonymous calls.</summary>
        protected string ActorId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        /// <summary>The signed-in user's username, or null for anonymous calls.</summary>
        protected string ActorName => User.GetUsername();
    }
}

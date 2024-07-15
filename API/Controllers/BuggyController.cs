using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Entities;
using API.Data;

namespace API.Controllers
{
    public class BuggyController : BaseApiController
    {
        // DataContext for accessing the database
        private readonly DataContext _context;

        public BuggyController(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns a secret text for authorized users.
        /// </summary>
        /// <returns>The secret text.</returns>
        [Authorize]
        [HttpGet("auth")]
        public ActionResult<string> GetSecret()
        {
            return "secret text";
        }

        /// <summary>
        /// Returns a 404 Not Found response.
        /// </summary>
        /// <returns>A 404 Not Found response.</returns>
        [HttpGet("not-found")]
        public ActionResult<AppUser> GetNotFound()
        {
            // Attempt to find a user with an invalid ID
            var thing = _context.Users.Find(-1);

            // Return a 404 Not Found response if the user is not found
            if (thing == null)
                return NotFound();

            // This code will never execute since the user with ID -1 does not exist
            return thing;
        }

        /// <summary>
        /// Returns a 500 Internal Server Error response.
        /// </summary>
        /// <returns>A 500 Internal Server Error response.</returns>
        [HttpGet("server-error")]
        public ActionResult<string> GetServerError()
        {
            // Attempt to find a user with an invalid ID
            var thing = _context.Users.Find(-1);

            // This will throw a NullReferenceException since the user with ID -1 does not exist
            var thingToReturn = thing.ToString();

            // This code will never execute due to the exception
            return thingToReturn;
        }

        /// <summary>
        /// Returns a 400 Bad Request response.
        /// </summary>
        /// <returns>A 400 Bad Request response.</returns>
        [HttpGet("bad-request")]
        public ActionResult<string> GetBadRequest()
        {
            // Return a 400 Bad Request response
            return BadRequest("This was not a good request");
        }
    }
}

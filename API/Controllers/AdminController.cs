using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        // UserManager for managing users
        private readonly UserManager<AppUser> _userManager;
        private readonly IAuditService _audit;

        public AdminController(UserManager<AppUser> userManager, IAuditService audit)
        {
            _userManager = userManager;
            _audit = audit;
        }

        /// <summary>
        /// Gets a list of users with their roles.
        /// </summary>
        /// <returns>A list of users with their roles.</returns>
        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            // Get users with their roles and order by username
            var users = await _userManager.Users
                .Include(r => r.UserRoles)
                .ThenInclude(r => r.Role)
                .OrderBy(u => u.UserName)
                .Select(u => new
                {
                    u.Id,
                    Username = u.UserName,
                    Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Edits the roles of a user.
        /// </summary>
        /// <param name="username">The username of the user to edit.</param>
        /// <param name="roles">The new roles for the user.</param>
        /// <returns>The updated roles for the user.</returns>
        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
        {
            if (string.IsNullOrWhiteSpace(roles))
                return BadRequest("You must select at least one role");

            // Split the roles string into an array
            var selectedRoles = roles.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();

            // Find the user by username
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound("Could not find user");

            // Get the current roles for the user
            var userRoles = await _userManager.GetRolesAsync(user);

            // Add the user to the new roles, excluding any existing roles
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded)
                return BadRequest("Failed to add to roles");

            // Remove the user from any roles not included in the new roles
            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
            if (!result.Succeeded)
                return BadRequest("Failed to remove from roles");

            var updatedRoles = await _userManager.GetRolesAsync(user);
            await _audit.RecordAsync(ActorId, ActorName, "user.roles.update", "User", user.Id.ToString(),
                $"Roles for {user.UserName}: {string.Join(", ", updatedRoles)}");

            // Return the updated roles for the user
            return Ok(updatedRoles);
        }

        /// <summary>
        /// Gets photos that need moderation.
        /// </summary>
        /// <returns>A message indicating that admins or moderators can see the photos.</returns>
        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public ActionResult GetPhotosForModeration()
        {
            return Ok("Admins or moderators can see this");
        }

    }
}

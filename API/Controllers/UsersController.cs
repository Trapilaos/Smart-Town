using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Entities;
using API.Interfaces;
using API.DTOs;
using Microsoft.AspNetCore.Identity;
using AutoMapper;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        // UserRepository for managing users
        private readonly IUserRepository _userRepository;
        // Mapper for mapping between entities and DTOs
        private readonly IMapper _mapper;
        // UserManager for managing users and roles
        private readonly UserManager<AppUser> _userManager;

        public UsersController(IUserRepository userRepository, IMapper mapper, UserManager<AppUser> userManager)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets all users with the "Admin" role.
        /// </summary>
        /// <returns>A list of admin users.</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsers()
        {
            // Get all users with the "Admin" role and map them to MemberDTO objects
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminDtos = _mapper.Map<IEnumerable<MemberDTO>>(admins);
            return Ok(adminDtos);
        }

        /// <summary>
        /// Gets a user by ID.
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <returns>The user with the specified ID.</returns>
        [HttpGet("id/{id}", Name = "GetUserById")]
        public async Task<ActionResult<MemberDTO>> GetUserById(int id)
        {
            // Get the user by ID and return a 404 Not Found response if the user is not found
            var user = await _userRepository.GetMemberByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        /// <summary>
        /// Gets the current user by username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The current user.</returns>
        [HttpGet("{username}", Name = "GetMemberByUsername")]
        public async Task<ActionResult<MemberDTO>> GetMemberByUsername(string username)
        {
            // Get the current user's username
            var currentUsername = User.Identity.Name;
            // Check if the requested username matches the current user's username
            if (username != currentUsername)
            {
                return Unauthorized();
            }

            // Get the user by username and return a 404 Not Found response if the user is not found
            var user = await _userRepository.GetMemberAsync(username);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
    }
}

using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        // AutoMapper for mapping between DTOs and entities
        private readonly IMapper _mapper;
        // UserManager for managing users
        private readonly UserManager<AppUser> _userManager;
        // TokenService for generating JWT tokens
        private readonly ITokenService _tokenService;
        // Logger for logging events
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user and assigns the "Member" role.
        /// </summary>
        /// <param name="registerDTO">The registration data.</param>
        /// <returns>The UserDTO with the username and token.</returns>
        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
        {
            // Check if the username is already taken
            if (await UserExists(registerDTO.Username))
                return BadRequest("Username is taken!");

            // Map the RegisterDTO to an AppUser entity
            var user = _mapper.Map<AppUser>(registerDTO);
            // Set the username to lowercase
            user.UserName = registerDTO.Username.ToLower();

            // Create the user with the provided password
            var result = await _userManager.CreateAsync(user, registerDTO.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Add the "Member" role to the user
            var roleResult = await _userManager.AddToRoleAsync(user, "Member");
            if (!roleResult.Succeeded)
                return BadRequest(result.Errors);

            // Return the UserDTO with the username and token
            return new UserDTO
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user)
            };
        }

        /// <summary>
        /// Logs in a user and returns the UserDTO with the username, token, and photo URL.
        /// </summary>
        /// <param name="loginDTO">The login data.</param>
        /// <returns>The UserDTO with the username, token, and photo URL.</returns>
        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {
            _logger.LogInformation("Login attempt for username: {Username}", loginDTO.Username);

            AppUser user;

            // Check if the user is an admin
            if (loginDTO.Username.Equals("Admin", System.StringComparison.OrdinalIgnoreCase))
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                user = adminUsers.FirstOrDefault();
            }
            else
            {
                // Get the user by the provided username
                user = await _userManager.Users
                    .Include(p => p.Photos)
                    .SingleOrDefaultAsync(x => x.UserName == loginDTO.Username.ToLower());
            }

            // Check if the user exists
            if (user == null)
            {
                _logger.LogWarning("Invalid username: {Username}", loginDTO.Username);
                return Unauthorized("Invalid Username!");
            }

            // Check if the provided password is correct
            var result = await _userManager.CheckPasswordAsync(user, loginDTO.Password);
            if (!result)
            {
                _logger.LogWarning("Invalid password for username: {Username}", loginDTO.Username);
                return Unauthorized("Invalid Password");
            }

            // Initialize Photos collection if null
            if (user.Photos == null)
            {
                _logger.LogWarning("Photos collection is null for user: {Username}", loginDTO.Username);
                user.Photos = new List<Photo>();
            }

            // Get the main photo for the user
            var mainPhoto = user.Photos.FirstOrDefault(x => x.IsMain);
            if (mainPhoto == null)
            {
                _logger.LogWarning("No main photo found for user: {Username}", loginDTO.Username);
            }

            _logger.LogInformation("Login successful for username: {Username}", loginDTO.Username);

            // Return the UserDTO with the username, token, and photo URL
            return new UserDTO
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user),
                PhotoUrl = mainPhoto?.Url
            };
        }

        /// <summary>
        /// Checks if a user with the given username exists.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <returns>True if the user exists, false otherwise.</returns>
        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}

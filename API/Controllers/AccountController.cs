using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using API.Services;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private const string InvalidCredentials = "Invalid username or password";

        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AccountController> _logger;
        private readonly LoginThrottle _loginThrottle;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            ITokenService tokenService, IMapper mapper, ILogger<AccountController> logger, LoginThrottle loginThrottle)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
            _loginThrottle = loginThrottle;
        }

        /// <summary>
        /// Registers a new user and assigns the "Member" role.
        /// </summary>
        [HttpPost("register")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
        {
            if (await UserExists(registerDTO.Username))
                return BadRequest("Username is taken!");

            var user = _mapper.Map<AppUser>(registerDTO);
            user.UserName = registerDTO.Username.ToLower();

            var result = await _userManager.CreateAsync(user, registerDTO.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var roleResult = await _userManager.AddToRoleAsync(user, "Member");
            if (!roleResult.Succeeded)
                return BadRequest(roleResult.Errors);

            return new UserDTO
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user)
            };
        }

        /// <summary>
        /// Logs a user in. Failures are throttled per username and client address, so one client cannot lock the
        /// account out for everyone else. The error message never reveals whether the username exists.
        /// </summary>
        [HttpPost("login")]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {
            if (string.IsNullOrWhiteSpace(loginDTO?.Username) || string.IsNullOrEmpty(loginDTO.Password))
                return Unauthorized(InvalidCredentials);

            // Usernames match exactly, ignoring case. There is no alias for any other account.
            var username = loginDTO.Username.ToLower();
            var clientAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (_loginThrottle.IsBlocked(username, clientAddress))
            {
                _logger.LogWarning("Login refused: too many failed attempts from this address");
                return Unauthorized(InvalidCredentials);
            }

            var user = await _userManager.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == username);

            if (user == null)
            {
                _loginThrottle.RecordFailure(username, clientAddress);
                _logger.LogWarning("Failed login: unknown username");
                return Unauthorized(InvalidCredentials);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDTO.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _loginThrottle.RecordFailure(username, clientAddress);
                _logger.LogWarning("Failed login for user id {UserId}", user.Id);
                return Unauthorized(InvalidCredentials);
            }

            _loginThrottle.Reset(username, clientAddress);

            var mainPhoto = user.Photos?.FirstOrDefault(x => x.IsMain);

            return new UserDTO
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user),
                PhotoUrl = mainPhoto?.Url
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}

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
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("register")] // POST : api/account/register
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
        {
            if (await UserExists(registerDTO.Username)) return BadRequest("Username is taken!");
            var user = _mapper.Map<AppUser>(registerDTO);

            user.UserName = registerDTO.Username.ToLower();
       
            var result = await _userManager.CreateAsync(user, registerDTO.Password);
            if(!result.Succeeded) return BadRequest(result.Errors);

            var roleResult = await _userManager.AddToRoleAsync(user, "Member");

            if(!roleResult.Succeeded) return BadRequest(result.Errors);
            return new UserDTO{
                Username = user.UserName,
                Token =await _tokenService.CreateToken(user)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {
            _logger.LogInformation("Login attempt for username: {Username}", loginDTO.Username);

            var user = await _userManager.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == loginDTO.Username.ToLower());

            if (user == null)
            {
                _logger.LogWarning("Invalid username: {Username}", loginDTO.Username);
                return Unauthorized("Invalid Username!");
            }

            var result = await _userManager.CheckPasswordAsync(user, loginDTO.Password);

            if (!result)
            {
                _logger.LogWarning("Invalid password for username: {Username}", loginDTO.Username);
                return Unauthorized("Invalid Password");
            }

            if (user.Photos == null)
            {
                _logger.LogWarning("Photos collection is null for user: {Username}", loginDTO.Username);
                user.Photos = new List<Photo>(); // Initialize to avoid null reference
            }

            var mainPhoto = user.Photos.FirstOrDefault(x => x.IsMain);
            if (mainPhoto == null)
            {
                _logger.LogWarning("No main photo found for user: {Username}", loginDTO.Username);
            }

            _logger.LogInformation("Login successful for username: {Username}", loginDTO.Username);

            return new UserDTO{
                Username = user.UserName,
                Token =await _tokenService.CreateToken(user),
                PhotoUrl = mainPhoto?.Url
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}

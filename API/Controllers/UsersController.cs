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
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;

        public UsersController(IUserRepository userRepository, IMapper mapper, UserManager<AppUser> userManager)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsers()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminDtos = _mapper.Map<IEnumerable<MemberDTO>>(admins);
            return Ok(adminDtos);
        }

        [HttpGet("id/{id}", Name = "GetUserById")]
        public async Task<ActionResult<MemberDTO>> GetUserById(int id)
        {
            var user = await _userRepository.GetMemberByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("{username}", Name = "GetMemberByUsername")]
        public async Task<ActionResult<MemberDTO>> GetMemberByUsername(string username)
        {
            var currentUsername = User.Identity.Name;
            if (username != currentUsername)
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetMemberAsync(username);
            if (user == null) return NotFound();
            return Ok(user);
        }
    }
}

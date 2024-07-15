using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public UserRepository(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets a member by their username.
        /// </summary>
        /// <param name="username">The username of the member.</param>
        /// <returns>The MemberDTO object for the specified username.</returns>
        public async Task<MemberDTO> GetMemberAsync(string username)
        {
            return await _context.Users
                .Where(x => x.UserName == username)
                .ProjectTo<MemberDTO>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets all members.
        /// </summary>
        /// <returns>A list of MemberDTO objects.</returns>
        public async Task<IEnumerable<MemberDTO>> GetMembersAsync()
        {
            return await _context.Users.ProjectTo<MemberDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        /// <summary>
        /// Gets a user by their ID.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <returns>The AppUser object for the specified ID.</returns>
        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        /// <summary>
        /// Gets a user by their username.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>The AppUser object for the specified username.</returns>
        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == username);
        }

        /// <summary>
        /// Gets all users.
        /// </summary>
        /// <returns>A list of AppUser objects.</returns>
        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _context.Users
                .Include(p => p.Photos)
                .ToListAsync();
        }

        /// <summary>
        /// Updates a user.
        /// </summary>
        /// <param name="user">The AppUser object to update.</param>
        public void Update(AppUser user)
        {
            _context.Entry(user).State = EntityState.Modified;
        }

        /// <summary>
        /// Gets a member by their ID.
        /// </summary>
        /// <param name="id">The ID of the member.</param>
        /// <returns>The MemberDTO object for the specified ID.</returns>
        public async Task<MemberDTO> GetMemberByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            return _mapper.Map<MemberDTO>(user);
        }
    }
}

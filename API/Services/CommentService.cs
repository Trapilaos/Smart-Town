using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class CommentService : ICommentService
    {
        private readonly DataContext _context;

        public CommentService(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a new comment to the database.
        /// </summary>
        /// <param name="newComment">The comment object to be added.</param>
        /// <returns>The newly created comment object.</returns>
        public async Task<Comment> AddCommentAsync(Comment newComment)
        {
            _context.Comments.Add(newComment);
            await _context.SaveChangesAsync();
            return newComment;
        }

        /// <summary>
        /// Retrieves all comments from the database.
        /// </summary>
        /// <returns>A list of comment objects.</returns>
        public async Task<IEnumerable<Comment>> GetCommentsAsync()
        {
            return await _context.Comments.ToListAsync();
        }
    }
}

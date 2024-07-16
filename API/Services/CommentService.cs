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

        public async Task<Comment> AddCommentAsync(Comment newComment)
        {
            _context.Comments.Add(newComment);
            await _context.SaveChangesAsync();
            return newComment;
        }

        public async Task<IEnumerable<Comment>> GetCommentsAsync()
        {
            return await _context.Comments.Where(c => !c.Seen).ToListAsync();
        }

        public async Task<Comment> MarkCommentAsSeenAsync(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment != null)
            {
                comment.Seen = true;
                await _context.SaveChangesAsync();
            }
            return comment;
        }
    }
}

using API.Data;
using API.DTOs;
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

        public async Task<IEnumerable<CommentDTO>> GetCommentsAsync()
        {
            var comments = await _context.Comments.Where(c => !c.Seen).ToListAsync();

            // Comment.UserId stores the author's id as text, so parse it to look up the usernames.
            var userIds = comments
                .Select(c => int.TryParse(c.UserId, out var id) ? id : 0)
                .Where(id => id != 0)
                .Distinct()
                .ToList();
            var usernames = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            return comments.Select(c => new CommentDTO
            {
                Id = c.Id,
                Content = c.Content,
                Date = c.Date,
                Seen = c.Seen,
                UserId = c.UserId,
                Username = int.TryParse(c.UserId, out var id) && usernames.TryGetValue(id, out var name) ? name : null
            }).ToList();
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

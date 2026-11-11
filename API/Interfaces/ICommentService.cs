using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface ICommentService
    {
        Task<Comment> AddCommentAsync(Comment newComment);
        Task<IEnumerable<CommentDTO>> GetCommentsAsync();
        Task<Comment> MarkCommentAsSeenAsync(int commentId); // Add this method
    }
}

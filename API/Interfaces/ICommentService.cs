using API.Entities;

namespace API.Interfaces
{
    public interface ICommentService
    {
        Task<Comment> AddCommentAsync(Comment newComment);
        Task<IEnumerable<Comment>> GetCommentsAsync();
        Task<Comment> MarkCommentAsSeenAsync(int commentId); // Add this method
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API.Entities;
using API.Interfaces;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        // CommentService for managing comments
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        /// <summary>
        /// Adds a new comment.
        /// </summary>
        /// <param name="newComment">The comment data.</param>
        /// <returns>The created comment.</returns>
        [HttpPost]
        [Authorize(Policy = "RequireMemberRole")]
        public async Task<IActionResult> AddComment([FromBody] Comment newComment)
        {
            // Set the user ID from the authenticated user's claim
            newComment.UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            // Set the current UTC date and time
            newComment.Date = DateTime.UtcNow;

            // Add the comment and return the created comment
            var createdComment = await _commentService.AddCommentAsync(newComment);
            return Ok(createdComment);
        }

        /// <summary>
        /// Gets all comments.
        /// </summary>
        /// <returns>A list of comments.</returns>
        [HttpGet]
        public async Task<IActionResult> GetComments()
        {
            // Get all comments and return them
            var comments = await _commentService.GetCommentsAsync();
            return Ok(comments);
        }
    }
}

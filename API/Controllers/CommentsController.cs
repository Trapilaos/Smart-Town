using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;

namespace API.Controllers
{
    public class CommentsController : BaseApiController
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpPost]
        [Authorize(Policy = "RequireMemberRole")]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentDTO dto)
        {
            // Server-controlled fields: author, timestamp and moderation state are never taken from the client.
            var comment = new Comment
            {
                Content = dto.Content.Trim(),
                UserId = User.GetUserId().ToString(),
                Date = DateTime.UtcNow,
                Seen = false
            };

            var createdComment = await _commentService.AddCommentAsync(comment);
            return Ok(createdComment);
        }

        // Moderation queue (unseen comments, with author ids and usernames): admin only.
        [HttpGet]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> GetComments()
        {
            var comments = await _commentService.GetCommentsAsync();
            return Ok(comments);
        }

        [HttpPost("{id}/mark-seen")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> MarkCommentAsSeen(int id)
        {
            var updatedComment = await _commentService.MarkCommentAsSeenAsync(id);
            if (updatedComment == null)
                return NotFound();

            return Ok(updatedComment);
        }
    }
}

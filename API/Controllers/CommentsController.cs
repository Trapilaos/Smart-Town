using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API.Entities;
using API.Interfaces;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : BaseApiController
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpPost]
        [Authorize(Policy = "RequireMemberRole")]
        public async Task<IActionResult> AddComment([FromBody] Comment newComment)
        {
            newComment.UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            newComment.Date = DateTime.UtcNow;

            var createdComment = await _commentService.AddCommentAsync(newComment);
            return Ok(createdComment);
        }

        [HttpGet]
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

using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class CreateCommentDTO
    {
        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Content { get; set; }
    }
}

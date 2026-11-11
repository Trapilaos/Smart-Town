using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class CreateEventDTO
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; }

        [StringLength(4000)]
        public string Description { get; set; }

        [StringLength(2048)]
        public string PhotoUrl { get; set; }

        public DateTime Date { get; set; }
    }
}

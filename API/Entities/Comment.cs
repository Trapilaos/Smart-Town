
namespace API.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string UserId { get; set; }
        public DateTime Date { get; set; }
        public bool Seen { get; set; }
    }
}
namespace API.DTOs
{
    // Moderation view of a comment: the author's username is joined in from Users.
    public class CommentDTO
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public bool Seen { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
    }
}

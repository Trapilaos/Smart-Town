
namespace API.Entities
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime Date { get; set; }
        public List<string> InterestedUsers { get; set; } = new List<string>();
    }
}
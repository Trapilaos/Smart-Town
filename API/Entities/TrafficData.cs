
namespace API.Entities
{
    public class TrafficData
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public int TrafficFlow { get; set; } 
        public DateTime Timestamp { get; set; }
    }
}
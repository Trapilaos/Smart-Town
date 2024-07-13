namespace API.DTOs
{
    public class LightingStatusDTO
    {
        public string Status { get; set; }
        public string Brightness { get; set; }
        public DateTime? NextOnTime { get; set; }
        public DateTime? NextOffTime { get; set; }
    }
}

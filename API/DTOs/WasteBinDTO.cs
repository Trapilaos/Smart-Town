namespace API.DTOs
{
    public class WasteBinDTO
    {
        public int Id { get; set; }
        public string Location { get; set; }
        public int Capacity { get; set; }
        public int CurrentFillLevel { get; set; }
        public DateTime LastEmptied { get; set; }
    }
}

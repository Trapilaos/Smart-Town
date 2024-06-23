
namespace API.Entities
{
    public class ParkingSpace
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MaxVehicles { get; set; }
        public int CurrentVehicles { get; set; }
    }
}
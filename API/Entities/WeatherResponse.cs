namespace API.Entities
{
    public class WeatherResponse
    {
        public CurrentWeather current { get; set; }
    }

    public class CurrentWeather
    {
        public Condition condition { get; set; }
        public float temp_c { get; set; }
        public int humidity { get; set; }
    }

    public class Condition
    {
        public string text { get; set; }
        public string icon { get; set; }
        public int code { get; set; }
    }
}
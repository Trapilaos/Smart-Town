namespace API.Entities
{
    public class WeatherResponse
    {
        public CurrentWeather current { get; set; }
        public Forecast forecast { get; set; }
    }

    public class CurrentWeather
    {
        public Condition condition { get; set; }
        public int is_day { get; set; }
        public int cloud { get; set; }
    }

    public class Condition
    {
        public string text { get; set; }
    }

    public class Forecast
    {
        public List<ForecastDay> forecastday { get; set; }
    }

    public class ForecastDay
    {
        public Astro astro { get; set; }
    }

    public class Astro
    {
        public string sunrise { get; set; }
        public string sunset { get; set; }
    }
}

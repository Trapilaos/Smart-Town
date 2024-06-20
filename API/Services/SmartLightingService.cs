using Newtonsoft.Json.Linq;

namespace API.Services
{
    public class SmartLightingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmartLightingService> _logger;

        public SmartLightingService(HttpClient httpClient, IConfiguration configuration, ILogger<SmartLightingService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(string, string)> GetLightingStatusAsync(string town)
        {
            var apiKey = _configuration["WeatherApi:ApiKey"];
            var baseUrl = _configuration["WeatherApi:BaseUrl"];
            var url = $"{baseUrl}/forecast.json?key={apiKey}&q={town}&days=1";

            _logger.LogInformation($"Fetching weather data from: {url}");

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error fetching weather data: {response.ReasonPhrase}");
                throw new HttpRequestException($"Error fetching weather data: {response.ReasonPhrase}");
            }

            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Weather API response data: {responseData}");
            var json = JObject.Parse(responseData);

            var condition = json["current"]["condition"]["text"].ToString().ToLower();
            var isDaytime = json["current"]["is_day"].ToObject<int>() == 1;

            var sunset = json["forecast"]["forecastday"][0]["astro"]["sunset"].ToString();
            var sunrise = json["forecast"]["forecastday"][0]["astro"]["sunrise"].ToString();

            var currentTime = DateTime.Now.TimeOfDay;
            var sunsetTime = DateTime.Parse(sunset).TimeOfDay;
            var sunriseTime = DateTime.Parse(sunrise).TimeOfDay;

            var lightingStatus = "Off";
            string brightnessLevel = string.Empty;

            // Adjust lighting status based on conditions and time of day
            if (currentTime < sunriseTime || currentTime > sunsetTime)
            {
                lightingStatus = "On";
                int brightness = GetBrightnessLevel(currentTime, sunsetTime, sunriseTime, condition);
                brightnessLevel = $"{brightness}%";
            }

            _logger.LogInformation($"Determined lighting status: {lightingStatus}, Brightness Level: {brightnessLevel}");

            return (lightingStatus, brightnessLevel);
        }

        private int GetBrightnessLevel(TimeSpan currentTime, TimeSpan sunsetTime, TimeSpan sunriseTime, string condition)
        {
            if (currentTime < sunriseTime || currentTime > sunsetTime)
            {
                if (currentTime < sunriseTime.Add(TimeSpan.FromMinutes(30)) || currentTime > sunsetTime.Add(TimeSpan.FromMinutes(30)))
                {
                    return 60; // Just before sunrise or just after sunset
                }
                else if (currentTime > sunsetTime.Add(TimeSpan.FromMinutes(30)) && currentTime <= sunsetTime.Add(TimeSpan.FromHours(1)))
                {
                    return 75; // After sunset
                }
                else
                {
                    return 100; // Fully dark
                }
            }

            return 0; // Daytime, lights off
        }
    }
}

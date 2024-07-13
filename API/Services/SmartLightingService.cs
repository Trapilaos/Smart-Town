using API.DTOs;
using API.Entities;
using Newtonsoft.Json.Linq;
using System;

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

        public async Task<LightingStatusDTO> GetLightingStatusAsync(string town)
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
            var weatherResponse = JObject.Parse(responseData).ToObject<WeatherResponse>();

            var condition = weatherResponse.current.condition.text.ToLower();
            var isDaytime = weatherResponse.current.is_day == 1;

            var sunset = DateTime.Parse(weatherResponse.forecast.forecastday[0].astro.sunset);
            var sunrise = DateTime.Parse(weatherResponse.forecast.forecastday[0].astro.sunrise);

            var currentDateTime = DateTime.Now;
            var sunsetDateTime = DateTime.Today.Add(sunset.TimeOfDay);
            var sunriseDateTime = DateTime.Today.Add(sunrise.TimeOfDay);

            var lightingStatusDTO = new LightingStatusDTO();

            if (sunsetDateTime < currentDateTime || sunriseDateTime > currentDateTime || IsWeatherMoody(condition))
            {
                lightingStatusDTO.Status = "On";
                lightingStatusDTO.Brightness = GetBrightnessLevel(currentDateTime, sunsetDateTime, sunriseDateTime, condition).ToString() + "%";
                lightingStatusDTO.NextOffTime = sunriseDateTime.Date.Add(sunrise.TimeOfDay);
            }
            else
            {
                lightingStatusDTO.Status = "Off";
                lightingStatusDTO.Brightness = null;
                lightingStatusDTO.NextOnTime = sunsetDateTime.Date.Add(sunset.TimeOfDay);
            }

            _logger.LogInformation($"Determined lighting status: {lightingStatusDTO.Status}, Brightness Level: {lightingStatusDTO.Brightness}");

            return lightingStatusDTO;
        }

        private int GetBrightnessLevel(DateTime currentDateTime, DateTime sunsetDateTime, DateTime sunriseDateTime, string condition)
        {
            if (IsWeatherMoody(condition))
            {
                return 100; // Weather is moody, always return 100
            }

            if (currentDateTime < sunriseDateTime || currentDateTime > sunsetDateTime)
            {
                if (currentDateTime >= sunriseDateTime.AddMinutes(-30) && currentDateTime < sunriseDateTime)
                {
                    return 60; // Just before sunrise
                }
                else if (currentDateTime > sunsetDateTime && currentDateTime <= sunsetDateTime.AddMinutes(30))
                {
                    return 75; // Just after sunset
                }
                else
                {
                    return 100; // Fully dark
                }
            }

            return 0; // Daytime, lights off
        }


        private bool IsWeatherMoody(string condition)
        {
            var moodyConditions = new List<string> { "cloudy", "overcast", "rain", "fog", "mist", "drizzle" };
            return moodyConditions.Any(condition.Contains);
        }
    }
}

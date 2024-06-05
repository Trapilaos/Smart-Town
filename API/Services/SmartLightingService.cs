using Newtonsoft.Json.Linq;

namespace API.Services
{
    public class SmartLightingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SmartLightingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetLightingStatusAsync(string town)
        {
            var apiKey = _configuration["WeatherApi:ApiKey"];
            var baseUrl = _configuration["WeatherApi:BaseUrl"];
            var url = $"{baseUrl}/current.json?key={apiKey}&q={town}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error fetching weather data: {response.ReasonPhrase}");
            }

            var responseData = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseData);

            // Extract necessary data from JSON response
            var condition = json["current"]["condition"]["text"].ToString();
            var lightingStatus = condition.Contains("clear") ? "Lights Off" : "Lights On";

            return lightingStatus;
        }
    }
}

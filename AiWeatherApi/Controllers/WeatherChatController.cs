using Microsoft.AspNetCore.Mvc;
using AiWeatherApi.Models;
using System.Text.Json;
using System.Globalization;
using OpenAI.Responses;

namespace AiWeatherApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherChatController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherChatController> _logger;
#pragma warning disable OPENAI001
    private readonly OpenAIResponseClient _openAiClient;
#pragma warning restore OPENAI001

    public WeatherChatController(
        IHttpClientFactory httpClientFactory, 
        ILogger<WeatherChatController> logger,
        IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;

        var apiKey = config["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Brakuje klucza OpenAI (User Secrets lub zmienna œrodowiskowa).");

#pragma warning disable OPENAI001
        _openAiClient = new OpenAIResponseClient(model: "gpt-4o-mini", apiKey: apiKey);
#pragma warning restore OPENAI001
    }

    [HttpGet("{city}")]
    public async Task<IActionResult> GetWeatherWithAiComment(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest("Nazwa miasta nie mo¿e byæ pusta.");

        try
        {
            // Krok 1: Pobranie danych pogodowych
            var weatherData = await GetWeatherDataAsync(city);
            if (weatherData == null)
                return NotFound($"Nie znaleziono danych pogodowych dla miasta: {city}");

            // Krok 2: Przygotowanie promptu dla AI
            var prompt = $@"Jesteœ sarkastycznym komentatorem pogody. Na podstawie poni¿szych danych pogodowych:

Miasto: {weatherData.City}
Temperatura: {weatherData.Temperature}°C
Prêdkoœæ wiatru: {weatherData.WindSpeed} km/h
Warunki: {weatherData.WeatherDescription}

Opisz pogodê w sarkastycznie humorystyczny sposób (2-3 zdania) i doradŸ, co za³o¿yæ. B¹dŸ dowcipny, ale pomocny.";

            // Krok 3: Wywo³anie OpenAI
            var aiResponse = await _openAiClient.CreateResponseAsync(prompt);
            var aiComment = aiResponse.Value.GetOutputText() ?? "AI nie mog³o wygenerowaæ komentarza.";

            // Krok 4: Zwrócenie po³¹czonej odpowiedzi
            var result = new WeatherChatResponse
            {
                City = weatherData.City,
                Temperature = weatherData.Temperature,
                WindSpeed = weatherData.WindSpeed,
                WeatherDescription = weatherData.WeatherDescription,
                AiComment = aiComment
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "B³¹d podczas przetwarzania ¿¹dania dla miasta: {City}", city);
            return StatusCode(500, "Wyst¹pi³ b³¹d podczas przetwarzania ¿¹dania.");
        }
    }

    private async Task<WeatherResponse?> GetWeatherDataAsync(string city)
    {
        try
        {
            // Krok 1: Geokodowanie
            var geocodingUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=pl&format=json";
            _logger.LogInformation("Pobieranie wspó³rzêdnych dla: {City}", city);
            
            var geocodingResponse = await _httpClient.GetAsync(geocodingUrl);
            if (!geocodingResponse.IsSuccessStatusCode)
                return null;

            var geocodingJson = await geocodingResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var geocodingData = JsonSerializer.Deserialize<GeocodingResponse>(geocodingJson, options);

            if (geocodingData?.Results == null || geocodingData.Results.Count == 0)
                return null;

            var location = geocodingData.Results[0];

            // Krok 2: Pobranie danych pogodowych
            var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
            var lon = location.Longitude.ToString(CultureInfo.InvariantCulture);
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,wind_speed_10m,weather_code";
            
            var weatherResponse = await _httpClient.GetAsync(weatherUrl);
            if (!weatherResponse.IsSuccessStatusCode)
                return null;

            var weatherJson = await weatherResponse.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<OpenMeteoResponse>(weatherJson, options);

            if (weatherData?.Current == null)
                return null;

            return new WeatherResponse
            {
                City = location.Name ?? city,
                Temperature = weatherData.Current.Temperature,
                WindSpeed = weatherData.Current.WindSpeed,
                WeatherCode = weatherData.Current.WeatherCode,
                WeatherDescription = GetWeatherDescription(weatherData.Current.WeatherCode)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "B³¹d podczas pobierania danych pogodowych");
            return null;
        }
    }

    private static string GetWeatherDescription(int code)
    {
        return code switch
        {
            0 => "Bezchmurnie",
            1 or 2 or 3 => "Czêœciowo pochmurno",
            45 or 48 => "Mg³a",
            51 or 53 or 55 => "M¿awka",
            61 or 63 or 65 => "Deszcz",
            71 or 73 or 75 => "Œnieg",
            77 => "Œnieg ziarnisty",
            80 or 81 or 82 => "Przelotne opady deszczu",
            85 or 86 => "Przelotne opady œniegu",
            95 => "Burza",
            96 or 99 => "Burza z gradem",
            _ => "Nieznane"
        };
    }
}

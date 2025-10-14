using Microsoft.AspNetCore.Mvc;
using AiWeatherApi.Models;
using System.Text.Json;
using System.Globalization;

namespace AiWeatherApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IHttpClientFactory httpClientFactory, ILogger<WeatherController> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    [HttpGet("{city}")]
    public async Task<IActionResult> GetWeather(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest("Nazwa miasta nie mo¿e byæ pusta.");

        try
        {
            // Krok 1: Geokodowanie - pobranie wspó³rzêdnych miasta
            var geocodingUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=pl&format=json";
            _logger.LogInformation("Pobieranie wspó³rzêdnych z: {Url}", geocodingUrl);
            
            var geocodingResponse = await _httpClient.GetAsync(geocodingUrl);
            
            if (!geocodingResponse.IsSuccessStatusCode)
                return StatusCode(500, "B³¹d podczas pobierania wspó³rzêdnych miasta.");

            var geocodingJson = await geocodingResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("OdpowiedŸ geocoding API: {Json}", geocodingJson);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var geocodingData = JsonSerializer.Deserialize<GeocodingResponse>(geocodingJson, options);

            if (geocodingData?.Results == null || geocodingData.Results.Count == 0)
            {
                _logger.LogWarning("Nie znaleziono wyników dla miasta: {City}", city);
                return NotFound($"Nie znaleziono miasta: {city}");
            }

            var location = geocodingData.Results[0];
            _logger.LogInformation("Znaleziono miasto: {Name} ({Lat}, {Lon})", location.Name, location.Latitude, location.Longitude);

            // Krok 2: Pobranie danych pogodowych - u¿ycie InvariantCulture dla poprawnego formatowania liczb
            var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
            var lon = location.Longitude.ToString(CultureInfo.InvariantCulture);
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,wind_speed_10m,weather_code";
            _logger.LogInformation("Pobieranie pogody z: {Url}", weatherUrl);
            
            var weatherResponse = await _httpClient.GetAsync(weatherUrl);

            if (!weatherResponse.IsSuccessStatusCode)
            {
                var errorContent = await weatherResponse.Content.ReadAsStringAsync();
                _logger.LogError("B³¹d odpowiedzi pogodowej: {StatusCode}, {Content}", weatherResponse.StatusCode, errorContent);
                return StatusCode(500, "B³¹d podczas pobierania danych pogodowych.");
            }

            var weatherJson = await weatherResponse.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<OpenMeteoResponse>(weatherJson, options);

            if (weatherData?.Current == null)
                return StatusCode(500, "Brak danych pogodowych.");

            // Krok 3: Przygotowanie odpowiedzi
            var result = new WeatherResponse
            {
                City = location.Name ?? city,
                Temperature = weatherData.Current.Temperature,
                WindSpeed = weatherData.Current.WindSpeed,
                WeatherCode = weatherData.Current.WeatherCode,
                WeatherDescription = GetWeatherDescription(weatherData.Current.WeatherCode)
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "B³¹d podczas pobierania danych pogodowych dla miasta: {City}", city);
            return StatusCode(500, "Wyst¹pi³ b³¹d podczas pobierania danych pogodowych.");
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

namespace AiWeatherApi.Models;

public class WeatherResponse
{
    public string City { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double WindSpeed { get; set; }
    public int WeatherCode { get; set; }
    public string WeatherDescription { get; set; } = string.Empty;
}

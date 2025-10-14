namespace AiWeatherApi.Models;

public class WeatherChatResponse
{
    public string City { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double WindSpeed { get; set; }
    public string WeatherDescription { get; set; } = string.Empty;
    public string AiComment { get; set; } = string.Empty;
}

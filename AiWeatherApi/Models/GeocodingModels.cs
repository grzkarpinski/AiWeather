using System.Text.Json.Serialization;

namespace AiWeatherApi.Models;

public class GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<GeocodingResult>? Results { get; set; }
}

public class GeocodingResult
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
    
    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

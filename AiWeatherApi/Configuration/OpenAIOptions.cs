using System.ComponentModel.DataAnnotations;

namespace AiWeatherApi.Configuration;

public class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    [Required(ErrorMessage = "Klucz API OpenAI jest wymagany.")]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = "gpt-4o-mini";
}

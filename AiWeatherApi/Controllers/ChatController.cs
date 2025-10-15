using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using AiWeatherApi.Models;
using AiWeatherApi.Configuration;

namespace AiWeatherApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OpenAIOptions _openAiOptions;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IOptions<OpenAIOptions> openAiOptions,
        ILogger<ChatController> logger)
    {
        _openAiOptions = openAiOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Pole 'Message' nie może być puste." });

        try
        {
#pragma warning disable OPENAI001
            var client = new OpenAIResponseClient(
                model: _openAiOptions.Model, 
                apiKey: _openAiOptions.ApiKey);
#pragma warning restore OPENAI001

            var result = await client.CreateResponseAsync(request.Message);
            var text = result.Value.GetOutputText() ?? "(Brak odpowiedzi)";
            
            _logger.LogInformation("Pomyślnie wygenerowano odpowiedź AI dla żądania o długości {Length} znaków", 
                request.Message.Length);
            
            return Ok(new { reply = text });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas generowania odpowiedzi AI");
            return StatusCode(500, new { error = "Wystąpił błąd podczas przetwarzania żądania." });
        }
    }
}

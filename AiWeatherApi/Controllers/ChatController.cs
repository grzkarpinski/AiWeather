using Microsoft.AspNetCore.Mvc;
using OpenAI.Responses;
using AiWeatherApi.Models;

namespace AiWeatherApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
#pragma warning disable OPENAI001
    private readonly OpenAIResponseClient _client;
#pragma warning restore OPENAI001

    public ChatController(IConfiguration config)
    {
        var apiKey = config["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Brakuje klucza OpenAI (User Secrets lub zmienna środowiskowa).");

#pragma warning disable OPENAI001
        _client = new OpenAIResponseClient(model: "gpt-4o-mini", apiKey: apiKey);
#pragma warning restore OPENAI001
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Pole 'Message' nie może być puste.");

        var result = await _client.CreateResponseAsync(request.Message);
        var text = result.Value.GetOutputText() ?? "(Brak odpowiedzi)";
        return Ok(new { reply = text });
    }
}

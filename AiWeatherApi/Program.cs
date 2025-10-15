using AiWeatherApi.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Konfiguracja Options pattern dla OpenAI
builder.Services.Configure<OpenAIOptions>(options =>
{
    var config = builder.Configuration;
    options.ApiKey = config["OpenAI:ApiKey"] 
        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
        ?? string.Empty;
    options.Model = config["OpenAI:Model"] ?? "gpt-4o-mini";
});

// Walidacja opcji przy starcie aplikacji
builder.Services.AddOptionsWithValidateOnStart<OpenAIOptions>()
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), 
        "Klucz API OpenAI nie zosta³ skonfigurowany. Ustaw go w User Secrets lub zmiennej œrodowiskowej OPENAI_API_KEY.");

// Dodanie CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://localhost:5001",
            "http://localhost:5000",
            "https://localhost:7001",
            "http://localhost:7000",
            "https://localhost:7097",
            "https://localhost:7179" // <-- dodano port Blazor
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

// Walidacja konfiguracji przy starcie
try
{
    var options = app.Services.GetRequiredService<IOptions<OpenAIOptions>>().Value;
    app.Logger.LogInformation("Konfiguracja OpenAI za³adowana pomyœlnie. Model: {Model}", options.Model);
}
catch (OptionsValidationException ex)
{
    app.Logger.LogCritical(ex, "B³¹d konfiguracji OpenAI. Aplikacja nie mo¿e zostaæ uruchomiona.");
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.Run();
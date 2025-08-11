using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using MyApi.DTOs;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Only wire up heavy services when not invoked by the build-time doc generator
var isOpenApiGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

if (!isOpenApiGeneration)
{
    // Register EF Core DbContext using the dedicated 'weather' database, if configured
    var weatherConnectionString = builder.Configuration.GetConnectionString("weather");
    var hasWeatherConnection = !string.IsNullOrWhiteSpace(weatherConnectionString);
    if (hasWeatherConnection)
    {
        builder.AddNpgsqlDbContext<MyApi.AppDbContext>("weather");
    }

    // Configure Semantic Kernel (reuse llm connection string provided by model reference)
    var llmCs = builder.Configuration.GetConnectionString("llm");
    if (!string.IsNullOrWhiteSpace(llmCs))
    {
        var parts = llmCs.Split(';', StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Split('=', 2, StringSplitOptions.TrimEntries))
                         .Where(p => p.Length == 2)
                         .ToDictionary(p => p[0].ToLowerInvariant(), p => p[1], StringComparer.OrdinalIgnoreCase);
        parts.TryGetValue("accesskey", out var apiKey);
        parts.TryGetValue("model", out var modelId);
        parts.TryGetValue("endpoint", out var endpoint);
        parts.TryGetValue("provider", out var provider);

        try
        {
            if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(modelId))
            {
                var kb = Kernel.CreateBuilder();
                if ((provider ?? "").Equals("openai", StringComparison.OrdinalIgnoreCase))
                {
                    kb.AddOpenAIChatCompletion(modelId!, apiKey!);
                }
                // (Could add other providers here)
                var kernel = kb.Build();
                builder.Services.AddSingleton(kernel);
                builder.Services.AddSingleton<MyApi.WeatherAnalysisService>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to configure Semantic Kernel: {ex.Message}");
        }
    }
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure JSON options for proper serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
    // Add converters for DateOnly
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

if (!isOpenApiGeneration)
{
    if (builder.Configuration.GetConnectionString("weather") != null)
    {
        builder.Services.AddHostedService<MyApi.EnsureWeatherDatabaseCreatedHostedService>();
    }
}

// Register the weather mapper
builder.Services.AddScoped<MyApi.IWeatherMapper, MyApi.WeatherMapper>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS
app.UseCors("AllowAll");

app.MapGet("/weatherforecast", WeatherHandlers.GetWeatherForecast)
    .WithName("GetWeatherForecast")
    .WithSummary("Get weather forecast")
    .WithDescription("Get weather forecast data for the next 5 days.")
    .Produces<WeatherForecastDto[]>(StatusCodes.Status200OK)
    .WithOpenApi();

// Analyze endpoint: accepts forecasts (domain or DTO) and returns LLM analysis
app.MapPost("/weatherforecast/analyze", async (
    [FromServices] MyApi.WeatherAnalysisService analysis,
    [FromBody] IEnumerable<WeatherForecastDto> body,
    CancellationToken ct) =>
{
    var list = body.ToList();
    if (list.Count == 0)
        return Results.BadRequest("No forecasts supplied");

    var analysisText = await analysis.AnalyzeAsync(list, ct);
    return Results.Ok(new { analysis = analysisText });
}).WithName("AnalyzeWeather");

// New: Get latest weather analysis
app.MapGet("/weatheranalysis/latest", async ([FromServices] MyApi.AppDbContext db, [FromServices] MyApi.IWeatherMapper mapper) =>
{
    var latest = await db.WeatherAnalyses
        .OrderByDescending(a => a.CreatedAt)
        .FirstOrDefaultAsync();
    if (latest is null) return Results.NotFound();
    return Results.Ok(mapper.ToDto(latest));
})
.WithName("GetLatestWeatherAnalysis")
.WithSummary("Get latest weather analysis")
.Produces<WeatherAnalysisDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithOpenApi();

app.Run();

// WeatherForecast entity is now in a separate file and tracked by EF Core (see WeatherForecast.cs)

public static class WeatherHandlers
{
    public static Results<Ok<WeatherForecastDto[]>, ProblemHttpResult> GetWeatherForecast(
        [FromServices] MyApi.AppDbContext db,
        HttpContext http,
        [FromServices] ILoggerFactory loggerFactory,
        [FromServices] MyApi.IWeatherMapper mapper,
        [FromServices] IServiceScopeFactory scopeFactory)
    {
        try
        {
            var logger = loggerFactory.CreateLogger("WeatherEndpoint");
            var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

            // Generate 5 new forecasts (not yet persisted)
            var generated = Enumerable.Range(1, 5).Select(index => new MyApi.WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = summaries[Random.Shared.Next(summaries.Length)]
            }).ToList();

            // Schedule persistence AFTER response using a NEW scoped DbContext
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var scopedDb = scope.ServiceProvider.GetRequiredService<MyApi.AppDbContext>();
                    await scopedDb.WeatherForecasts.AddRangeAsync(generated);
                    var saved = await scopedDb.SaveChangesAsync();
                    logger.LogInformation("Persisted {Count} weather forecasts post-response", saved);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to persist weather forecasts");
                }
            });

            // Return DTOs (omit Id since not yet saved)
            var dto = generated.Select(mapper.ToDto).ToArray();
            return TypedResults.Ok(dto);
        }
        catch (Exception)
        {
            return TypedResults.Problem("An error occurred while processing your request.");
        }
    }
}
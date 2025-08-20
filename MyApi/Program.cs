using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
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
                kb.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
                if ((provider ?? "").Equals("openai", StringComparison.OrdinalIgnoreCase))
                {
                    kb.AddOpenAIChatCompletion(modelId!, apiKey!);
                }
                // (Could add other providers here)
                var kernel = kb.Build();
                builder.Services.AddSingleton(kernel);
                builder.Services.AddSingleton<MyApi.WeatherAnalysisService>();
                builder.Services.AddScoped<MyApi.StudyService>();
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

// Register endpoints from Presentation layer
MyApi.WeatherEndpoints.RegisterWeatherEndpoints(app);
MyApi.StudyEndpoints.RegisterStudyEndpoints(app);
MyApi.TutorEndpoints.RegisterTutorEndpoints(app);

app.Run();

// WeatherForecast entity is now in a separate file and tracked by EF Core (see WeatherForecast.cs)

// handlers moved to MyApi.Presentation.Endpoints.WeatherEndpoints
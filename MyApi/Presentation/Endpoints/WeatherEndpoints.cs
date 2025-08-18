using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MyApi;

public static class WeatherEndpoints
{
    public static void RegisterWeatherEndpoints(this WebApplication app)
    {
        app.MapGet("/weatherforecast", WeatherHandlers.GetWeatherForecast)
           .WithName("GetWeatherForecast")
           .WithSummary("Get weather forecast")
           .WithDescription("Get weather forecast data for the next 5 days.")
            .Produces<MyApi.WeatherForecastDto[]>(StatusCodes.Status200OK)
           .WithOpenApi();

        app.MapGet("/weatherforecast/generate", WeatherHandlers.GenerateForecast)
           .WithName("GenerateWeatherForecast")
           .WithSummary("Generate a plausible multi-day forecast using Semantic Kernel")
           .Produces<string>(StatusCodes.Status200OK)
           .WithOpenApi();

        app.MapPost("/weatherforecast/analyze", async (
            [FromServices] MyApi.WeatherAnalysisService analysis,
            [FromBody] IEnumerable<MyApi.WeatherForecastDto> body,
            CancellationToken ct) =>
        {
            var list = body.ToList();
            if (list.Count == 0)
                return Results.BadRequest("No forecasts supplied");

            var analysisText = await analysis.AnalyzeAsync(list, ct);
            return Results.Ok(new { analysis = analysisText });
        }).WithName("AnalyzeWeather");

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
        .Produces<MyApi.WeatherAnalysisDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();

        app.MapPost("/weatherforecast/study", WeatherHandlers.Study)
           .WithName("WeatherStudy")
           .WithSummary("Interactive weather study using Semantic Kernel")
           .Produces<object>(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status400BadRequest)
           .WithOpenApi();
    }

    // Handlers for complex GET
    public static class WeatherHandlers
    {
        public static Results<Ok<WeatherForecastDto[]>, ProblemHttpResult> GetWeatherForecast(
            [FromServices] AppDbContext db,
            HttpContext http,
            [FromServices] ILoggerFactory loggerFactory,
            [FromServices] IWeatherMapper mapper,
            [FromServices] IServiceScopeFactory scopeFactory)
        {
            try
            {
                var logger = loggerFactory.CreateLogger("WeatherEndpoint");
                var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

                var generated = Enumerable.Range(1, 5).Select(index => new MyApi.WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                }).ToList();

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

                var dto = generated.Select(mapper.ToDto).ToArray();
                return TypedResults.Ok(dto);
            }
            catch (Exception)
            {
                return TypedResults.Problem("An error occurred while processing your request.");
            }
        }

        public static async Task<IResult> GenerateForecast([FromServices] MyApi.WeatherAnalysisService analysis, CancellationToken ct)
        {
            var json = await analysis.GenerateForecastAsync(ct: ct);
            return TypedResults.Ok(json);
        }

        public static async Task<IResult> Study(
            [FromServices] MyApi.WeatherAnalysisService analysis,
            [FromServices] IServiceProvider sp,
            [FromBody] MyApi.WeatherStudyRequest request,
            CancellationToken ct)
        {
            if (request.Forecasts is null || request.Forecasts.Count == 0)
                return TypedResults.BadRequest("No forecasts supplied");

            // LLM intro via Semantic Kernel
            var text = await analysis.StudyIntroAsync(request.Forecasts, request.UserPrompt, ct);

            // Attach available SK plugin function names for the client to choose from
            var availableFunctions = new[]
            {
                "summarize_trends",
                "suggest_outfit",
                "plan_outdoor_activity",
                "pack_travel_list"
            };

            return TypedResults.Ok(new { llmAnalysis = text, availableFunctions });
        }
    }
}



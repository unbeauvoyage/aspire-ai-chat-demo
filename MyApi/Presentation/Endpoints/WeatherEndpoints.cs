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
    }

    // Handlers for complex GET
    public static class WeatherHandlers
    {
        public static Results<Ok<MyApi.WeatherForecastDto[]>, ProblemHttpResult> GetWeatherForecast(
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
    }
}



using Microsoft.SemanticKernel;
using System.Text;

using Shared;

namespace MyApi;

public class WeatherAnalysisService
{
    private readonly Kernel _kernel;

    public WeatherAnalysisService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> AnalyzeAsync(IEnumerable<WeatherForecast> forecasts, CancellationToken ct = default)
    {
        var prompt = WeatherAnalysis.Analyze(forecasts);
        var result = await _kernel.InvokePromptAsync<string>(prompt, cancellationToken: ct);
        return result ?? "(no analysis)";
    }

    public async Task<string> GenerateForecastAsync(int days = 5, CancellationToken ct = default)
    {
        var prompt = $"Generate a plausible {days}-day weather forecast as JSON array with fields: date (yyyy-MM-dd), temperatureC, summary. Keep values realistic.";
        var json = await _kernel.InvokePromptAsync<string>(prompt, cancellationToken: ct) ?? "[]";
        return json;
    }

    public async Task<string> StudyIntroAsync(IEnumerable<WeatherForecastDto> forecasts, string? userPrompt, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a helpful weather study assistant. Summarize key points and propose next actions.");
        foreach (var f in forecasts)
        {
            sb.AppendLine($"- {f.Date}: {f.TemperatureC}C ({f.TemperatureF}F) {f.Summary}");
        }
        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            sb.AppendLine($"User: {userPrompt}");
        }
        var text = await _kernel.InvokePromptAsync<string>(sb.ToString(), cancellationToken: ct);
        return text ?? string.Empty;
    }
}

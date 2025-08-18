using Microsoft.SemanticKernel;
using System.Text;

namespace MyApi;

public class WeatherAnalysisService
{
    private readonly Kernel _kernel;

    public WeatherAnalysisService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> AnalyzeAsync(IEnumerable<WeatherForecastDto> forecasts, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Provide a concise human-friendly analysis of the following upcoming weather forecasts. Summarize trends, extremes, and give advice.");
        sb.AppendLine();
        foreach (var f in forecasts)
        {
            sb.AppendLine($"- {f.Date:yyyy-MM-dd}: {f.TemperatureC}C ({f.TemperatureF}F) {(string.IsNullOrWhiteSpace(f.Summary) ? "(no summary)" : f.Summary)}");
        }
        sb.AppendLine();
        sb.AppendLine("Return 3-5 bullet points.");

        // Simple prompt invocation
        var result = await _kernel.InvokePromptAsync<string>(sb.ToString(), cancellationToken: ct);
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

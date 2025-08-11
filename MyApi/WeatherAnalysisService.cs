using Microsoft.SemanticKernel;
using System.Text;
using MyApi.DTOs;

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
        sb.AppendLine("Provide a concise human-friendly analysis of the following upcoming weather forecasts. Summarize trends, extremes, and give advice." );
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
}

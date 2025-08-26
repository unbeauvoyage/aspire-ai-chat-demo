namespace MyApi;


using System.Text;
using System.Collections.Generic;

public class WeatherAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Analysis { get; set; } = string.Empty;

    // Move Analyze logic here
    public static string Analyze(IEnumerable<WeatherForecast> forecasts)
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
        return sb.ToString();
    }
}





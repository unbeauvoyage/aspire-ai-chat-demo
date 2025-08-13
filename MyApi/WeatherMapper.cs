namespace MyApi;


public interface IWeatherMapper
{
    WeatherForecastDto ToDto(WeatherForecast source);
    WeatherAnalysisDto ToDto(WeatherAnalysis source);
}

public class WeatherMapper : IWeatherMapper
{
    public WeatherForecastDto ToDto(WeatherForecast source)
    {
        return new WeatherForecastDto(
            Date: source.Date.ToString("yyyy-MM-dd"),
            TemperatureC: source.TemperatureC,
            TemperatureF: source.TemperatureF
            // Summary: source.Summary
        );
    }

    public WeatherAnalysisDto ToDto(WeatherAnalysis source)
    {
        return new WeatherAnalysisDto(
            Id: source.Id,
            CreatedAt: source.CreatedAt,
            Analysis: source.Analysis
        );
    }
}

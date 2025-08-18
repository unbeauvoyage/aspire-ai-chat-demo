namespace MyApi;

public record WeatherStudyRequest(
    IReadOnlyList<WeatherForecastDto> Forecasts,
    string? UserPrompt = null
);



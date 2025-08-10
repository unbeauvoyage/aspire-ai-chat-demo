namespace MyApi;

public interface IWeatherMapper
{
    WeatherForecastDto ToDto(WeatherForecast source);
}

public class WeatherMapper : IWeatherMapper
{
    public WeatherForecastDto ToDto(WeatherForecast source)
    {
        return new WeatherForecastDto(
            Date: source.Date.ToString("yyyy-MM-dd"),
            TemperatureC: source.TemperatureC,
            TemperatureF: source.TemperatureF,
            Summary: source.Summary
        );
    }
}

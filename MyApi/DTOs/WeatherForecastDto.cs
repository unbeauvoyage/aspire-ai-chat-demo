using System.ComponentModel.DataAnnotations;

namespace MyApi.DTOs;

public record WeatherForecastDto(
    [property: Required] string Date, 
    [property: Required] int TemperatureC, 
    [property: Required] int TemperatureF, 
    string? Summary,
    string? IconD = null);
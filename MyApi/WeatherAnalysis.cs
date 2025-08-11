namespace MyApi;

public class WeatherAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Analysis { get; set; } = string.Empty;
}





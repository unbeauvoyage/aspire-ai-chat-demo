namespace MyApi;

public class StudyGoal
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}



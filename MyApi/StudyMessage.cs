namespace MyApi;

public class StudyMessage
{
    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = "assistant"; // "user" | "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public StudySession? Session { get; set; }
}



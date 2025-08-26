namespace MyApi;

public class QuizQuestion
{
    public int Id { get; set; }
    public int ConceptId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}



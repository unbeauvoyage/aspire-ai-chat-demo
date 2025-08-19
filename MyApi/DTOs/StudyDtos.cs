namespace MyApi;

public record StudyMessageDto(string Role, string Content, DateTime TimestampUtc);
public record StudySessionDto(Guid Id, string? Topic, IReadOnlyList<StudyMessageDto> Messages);
public record StartStudyRequest(string Topic, string? Level = null, string? Exam = null);
public record SendMessageRequest(Guid SessionId, string Message);



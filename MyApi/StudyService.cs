using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;

namespace MyApi;

public class StudyService
{
    private readonly Kernel _kernel;
    private readonly AppDbContext _db;

    public StudyService(Kernel kernel, AppDbContext db)
    {
        _kernel = kernel;
        _db = db;
    }

    public async Task<StudySessionDto> StartAsync(string topic, string? level, string? exam, CancellationToken ct)
    {
        var session = new StudySession { Topic = topic };
        _db.StudySessions.Add(session);

        var intro = $"We will study {topic}. Ask ALL of these in ONE question: user's current level, their goal, exam (if any). After their reply, proceed directly to teaching without further meta-questions. If exam is given (e.g., {exam}), use that exam's official format for questions. Keep one clear multi-part question now.";
        _db.StudyMessages.Add(new StudyMessage { SessionId = session.Id, Role = "assistant", Content = intro });
        await _db.SaveChangesAsync(ct);

        return await GetSessionDtoAsync(session.Id, ct);
    }

    public async Task<StudySessionDto> SendAsync(Guid sessionId, string userMessage, CancellationToken ct)
    {
        var exists = await _db.StudySessions.FindAsync(new object?[] { sessionId }, ct) != null;
        if (!exists) throw new InvalidOperationException("Session not found");

        _db.StudyMessages.Add(new StudyMessage { SessionId = sessionId, Role = "user", Content = userMessage, TimestampUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);

        var messages = await _db.StudyMessages.Where(m => m.SessionId == sessionId).OrderBy(m => m.Id).ToListAsync(ct);
        var history = string.Join("\n", messages.Select(m => $"{m.Role.ToUpperInvariant()}: {m.Content}"));

        var prompt = $@"You are a decisive study coach. Based on the user's single reply, immediately begin teaching.
- If an exam type (e.g., TOEFL) is detected, generate exercises strictly in that exam's format and scoring style.
- Otherwise, infer level and objective, pick a syllabus and start with a short teaching chunk and 3-5 questions.
- No further meta-questions about preferences; pick a path and proceed.

Conversation so far:
{history}

ASSISTANT:";

        var reply = await _kernel.InvokePromptAsync<string>(prompt, cancellationToken: ct) ?? string.Empty;
        _db.StudyMessages.Add(new StudyMessage { SessionId = sessionId, Role = "assistant", Content = reply.Trim(), TimestampUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);

        return await GetSessionDtoAsync(sessionId, ct);
    }

    private async Task<StudySessionDto> GetSessionDtoAsync(Guid sessionId, CancellationToken ct)
    {
        var msgs = await _db.StudyMessages.Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Id)
            .Select(m => new StudyMessageDto(m.Role, m.Content, m.TimestampUtc))
            .ToListAsync(ct);
        var topic = await _db.StudySessions.Where(s => s.Id == sessionId).Select(s => s.Topic).FirstOrDefaultAsync(ct);
        return new StudySessionDto(sessionId, topic, msgs);
    }
}



using Microsoft.SemanticKernel;
using System.Collections.Concurrent;

namespace MyApi;

public class StudyService
{
    private readonly Kernel _kernel;
    private readonly ConcurrentDictionary<Guid, List<StudyMessageDto>> _sessions = new();

    public StudyService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public StudySessionDto Start(string topic, string? level, string? exam)
    {
        var id = Guid.NewGuid();
        var messages = new List<StudyMessageDto>();
        _sessions[id] = messages;

        var intro = $"We will study {topic}. Ask concise questions to learn the user's current level and goals. If an exam is mentioned (e.g., {exam}), gather format and resources. Keep messages short.";
        messages.Add(new("assistant", intro, DateTime.UtcNow));
        return new(id, topic, messages);
    }

    public async Task<StudySessionDto> SendAsync(Guid sessionId, string userMessage, CancellationToken ct)
    {
        if (!_sessions.TryGetValue(sessionId, out var messages))
        {
            throw new InvalidOperationException("Session not found");
        }
        messages.Add(new("user", userMessage, DateTime.UtcNow));

        var history = string.Join("\n", messages.Select(m => $"{m.Role.ToUpperInvariant()}: {m.Content}"));
        var prompt = $@"You are a coaching assistant. Continue the dialog to assess user's current level, aim, and exam details if any. Be brief, ask one question at a time.

Conversation so far:
{history}

ASSISTANT:";

        var reply = await _kernel.InvokePromptAsync<string>(prompt, cancellationToken: ct) ?? string.Empty;
        messages.Add(new("assistant", reply.Trim(), DateTime.UtcNow));
        var topic = messages.FirstOrDefault()?.Content.Contains("study") == true ? "study" : null;
        return new(sessionId, topic, messages);
    }
}



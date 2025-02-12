using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddChatClient("llm");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/api/chat", (IChatClient chatClient, ChatMessage m) =>
{
    async IAsyncEnumerable<string?> GetMessages([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var update in chatClient.CompleteStreamingAsync(m.Text, cancellationToken: ct))
        {
            yield return update.Text;
        }
    }

    return Results.Extensions.SseStream(GetMessages);
});

app.Run();

public record ChatMessage(string Text);

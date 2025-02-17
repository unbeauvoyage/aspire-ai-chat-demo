using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

public static class ChatExtensions
{
    public static void MapChatApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat");

        group.MapGet("/", (AppDbContext db) =>
        {
            return db.Conversations.ToListAsync();
        });

        group.MapGet("/{id}", async (Guid id, AppDbContext db) =>
        {
            var conversation = await db.Conversations.FindAsync(id);

            if (conversation is null)
            {
                return Results.NotFound();
            }

            var clientMessages = from m in conversation.Messages
                                 select new ClientMessage(m.Id, m.Role, m.Text);

            return Results.Ok(clientMessages);
        });

        group.MapPost("/stream/{id}", (Guid id, StreamContext streamContext, ChatStreamingCoodinator streaming, CancellationToken token) =>
        {
            async IAsyncEnumerable<ClientMessageFragment> StreamMessages()
            {
                await foreach (var message in streaming.GetMessageStream(id, streamContext.LastMessageId).WithCancellation(token))
                {
                    yield return message;
                    await Task.Yield();
                }
            }

            return StreamMessages();
        });

        group.MapPost("/", async (NewConversation newConversation, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(newConversation.Name))
            {
                return Results.BadRequest();
            }

            var conversation = new Conversation
            {
                Id = Guid.CreateVersion7(),
                Name = newConversation.Name,
                Messages = []
            };

            db.Conversations.Add(conversation);
            await db.SaveChangesAsync();

            return Results.Created($"/api/chats/{conversation.Id}", conversation);
        });

        group.MapPost("/{id}", async (Guid id, AppDbContext db, IChatClient chatClient, Prompt prompt, CancellationToken token, ChatStreamingCoodinator streaming) =>
        {
            var conversation = await db.Conversations.FindAsync(id, token);

            if (conversation is null)
            {
                return Results.NotFound();
            }

            conversation.Messages.Add(new()
            {
                Id = Guid.CreateVersion7(),
                Role = ChatRole.User.Value,
                Text = prompt.Text
            });

            // This is inefficient
            var messages = conversation.Messages
                .Select(m => new ChatMessage(new(m.Role), m.Text))
                .ToList();

            streaming.AddStreamingMessage(id, Guid.CreateVersion7(), messages);

            return Results.Ok();
        });

        group.MapDelete("/{id}", async (Guid id, AppDbContext db) =>
        {
            var conversation = await db.Conversations.FindAsync(id);

            if (conversation is null)
            {
                return Results.NotFound();
            }

            db.Conversations.Remove(conversation);
            await db.SaveChangesAsync();

            return Results.Ok();
        });
    }
}

public record Prompt(string Text);

public record NewConversation(string Name);

public record ClientMessage(Guid Id, string Sender, string Text);

public record ClientMessageFragment(Guid Id, int Index, string Text);

public record StreamContext(Guid? LastMessageId);
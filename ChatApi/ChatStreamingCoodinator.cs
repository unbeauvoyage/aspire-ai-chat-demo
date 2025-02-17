using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.AI;


public class ChatStreamingCoodinator(
    IChatClient chatClient,
    IServiceScopeFactory scopeFactory,
    ILogger<ChatStreamingCoodinator> logger)
{
    private readonly ConcurrentDictionary<Guid, List<ClientMessageFragment>> _cache = [];
    private event Action<Guid, ClientMessageFragment>? OnMessage;

    public void AddStreamingMessage(Guid conversationId, Guid assistantReplyId, List<ChatMessage> messages)
    {
        logger.LogInformation("Adding streaming message for conversation {ConversationId}", conversationId);

        async Task StreamMessages()
        {
            var allChunks = new List<StreamingChatCompletionUpdate>();

            var backlog = _cache.GetOrAdd(conversationId, _ => []);

            lock (backlog)
            {
                backlog.Clear();
            }

            try
            {
                logger.LogInformation("Streaming message for conversation {ConversationId}", conversationId);

                await foreach (var update in chatClient.CompleteStreamingAsync(messages))
                {
                    if (update.Text is not null)
                    {
                        allChunks.Add(update);

                        var fragment = new ClientMessageFragment(assistantReplyId, allChunks.Count, update.Text);

                        lock (backlog)
                        {
                            backlog.Add(fragment);
                        }

                        OnMessage?.Invoke(conversationId, fragment);
                    }
                }

                logger.LogInformation("Full message received for conversation {ConversationId}", conversationId);
            }
            catch (Exception ex)
            {
                var fragment = new ClientMessageFragment(assistantReplyId, 0, "Error streaming message");

                lock (backlog)
                {
                    backlog.Add(fragment);
                }

                OnMessage?.Invoke(conversationId, fragment);

                logger.LogError(ex, "Error streaming message for conversation {ConversationId}", conversationId);
            }
            finally
            {
                var fullMessage = allChunks.ToChatCompletion().Message;

                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var conversation = await db.Conversations.FindAsync(conversationId);

                if (conversation is not null)
                {
                    conversation.Messages.Add(new ConversationChatMessage()
                    {
                        Id = assistantReplyId,
                        Role = fullMessage.Role.Value,
                        Text = fullMessage.Text!
                    });

                    await db.SaveChangesAsync();
                }
            }
        }

        // TODO: Make sure there's one stream per conversation
        _ = StreamMessages();
    }

    public async IAsyncEnumerable<ClientMessageFragment> GetMessageStream(Guid conversationId, Guid? lastMessageId)
    {
        logger.LogInformation("Getting message stream for conversation {ConversationId}, {LastMessageId}", conversationId, lastMessageId);

        var channel = Channel.CreateUnbounded<ClientMessageFragment>();

        void WriteToChannel(Guid id, ClientMessageFragment message)
        {
            if (id != conversationId)
            {
                return;
            }

            // Replay the backlog
            channel.Writer.TryWrite(message);
        }

        OnMessage += WriteToChannel;

        // TODO: Make sure we don't return duplicate messages
        if (_cache.TryGetValue(conversationId, out var backlog))
        {
            lock (backlog)
            {
                foreach (var m in backlog)
                {
                    channel.Writer.TryWrite(m);
                }
            }
        }

        await foreach (var message in channel.Reader.ReadAllAsync())
        {
            if (message.Id == lastMessageId)
            {
                continue;
            }

            yield return message;
        }

        OnMessage -= WriteToChannel;
    }
}
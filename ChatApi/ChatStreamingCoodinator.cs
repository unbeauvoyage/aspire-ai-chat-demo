using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.AI;


public class ChatStreamingCoodinator(IChatClient chatClient, IServiceScopeFactory scopeFactory, ILogger<ChatStreamingCoodinator> logger)
{
    private readonly ConcurrentDictionary<Guid, List<ClientMessageFragment>> _cache = [];
    private event Action<Guid, ClientMessageFragment>? OnMessage;

    public void AddStreamingMessage(Guid conversationId, Guid assistantReplyId, List<ChatMessage> messages)
    {
        logger.LogInformation("Adding streaming message for conversation {ConversationId}", conversationId);

        async IAsyncEnumerable<ClientMessageFragment> StreamMessages()
        {
            try
            {
                var allChunks = new List<StreamingChatCompletionUpdate>();

                var backlog = _cache.GetOrAdd(conversationId, _ => []);

                lock (backlog)
                {
                    backlog.Clear();
                }

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

                var fullMessage = allChunks.ToChatCompletion().Message;

                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var conversation = await db.Conversations.FindAsync(conversationId);

                if (conversation is null)
                {
                    yield break;
                }

                var assistantMessage = conversation.Messages.Find(m => m.Id == assistantReplyId);

                if (assistantMessage is null)
                {
                    yield break;
                }

                assistantMessage.Text = fullMessage.Text!;

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error streaming message for conversation {ConversationId}", conversationId);
            }
        }

        // TODO: Make sure there's one stream per conversation
        _ = Task.Run(async () =>
        {
            await foreach (var _ in StreamMessages()) { }
        });
    }

    public async IAsyncEnumerable<ClientMessageFragment> GetMessageStream(Guid conversationId)
    {
        logger.LogInformation("Getting streaming message for conversation {ConversationId}", conversationId);

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

        // REVIEW: You can get dupes
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

        var latestSeen = -1;

        await foreach (var message in channel.Reader.ReadAllAsync())
        {
            if (message.Index > latestSeen)
            {
                latestSeen = message.Index;
            }
            else
            {
                continue;
            }

            yield return message;
        }

        OnMessage -= WriteToChannel;
    }
}
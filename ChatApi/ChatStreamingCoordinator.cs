using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.AI;

public class ChatStreamingCoordinator(
    IChatClient chatClient,
    IServiceScopeFactory scopeFactory,
    ILogger<ChatStreamingCoordinator> logger) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, List<ClientMessageFragment>> _cache = [];
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _streamLocks = [];
    private readonly object _eventLock = new();
    private Action<Guid, ClientMessageFragment>? OnMessage;

    public async Task AddStreamingMessage(Guid conversationId, Guid assistantReplyId, List<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Adding streaming message for conversation {ConversationId}", conversationId);

        var streamLock = _streamLocks.GetOrAdd(conversationId, _ => new SemaphoreSlim(1, 1));
        await streamLock.WaitAsync(cancellationToken);

        try
        {
            await StreamMessages();
        }
        finally
        {
            streamLock.Release();
        }

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
                await foreach (var update in chatClient.CompleteStreamingAsync(messages).WithCancellation(cancellationToken))
                {
                    if (update.Text is not null)
                    {
                        allChunks.Add(update);
                        var fragment = new ClientMessageFragment(assistantReplyId, update.Text, Guid.CreateVersion7());

                        lock (backlog)
                        {
                            backlog.Add(fragment);
                        }

                        NotifySubscribers(conversationId, fragment);
                    }
                }

                logger.LogInformation("Full message received for conversation {ConversationId}", conversationId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var fragment = new ClientMessageFragment(assistantReplyId, "Error streaming message", Guid.CreateVersion7());
                lock (backlog)
                {
                    backlog.Add(fragment);
                }

                NotifySubscribers(conversationId, fragment);
                logger.LogError(ex, "Error streaming message for conversation {ConversationId}", conversationId);
            }
            finally
            {
                if (allChunks.Count > 0)
                {
                    var fullMessage = allChunks.ToChatCompletion().Message;
                    await SaveMessageToDatabase(conversationId, assistantReplyId, fullMessage);
                }

                // Only remove the cache entry, keep the lock
                _cache.TryRemove(conversationId, out _);
            }
        }
    }

    private async Task SaveMessageToDatabase(Guid conversationId, Guid messageId, ChatMessage message)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var conversation = await db.Conversations.FindAsync(conversationId);
        if (conversation is not null)
        {
            conversation.Messages.Add(new ConversationChatMessage
            {
                Id = messageId,
                Role = message.Role.Value,
                Text = message.Text!
            });

            await db.SaveChangesAsync();
        }
    }

    private void NotifySubscribers(Guid conversationId, ClientMessageFragment fragment)
    {
        Action<Guid, ClientMessageFragment>? handlers;
        lock (_eventLock)
        {
            handlers = OnMessage;
        }
        handlers?.Invoke(conversationId, fragment);
    }

    public async IAsyncEnumerable<ClientMessageFragment> GetMessageStream(
        Guid conversationId, 
        Guid? lastMessageId, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting message stream for conversation {ConversationId}, {LastMessageId}", conversationId, lastMessageId);

        var channel = Channel.CreateUnbounded<ClientMessageFragment>();

        // Track the last delivered fragment separately, this de-dupes fragments from the same message.
        var lastDeliveredFragment = Guid.Empty;

        void WriteToChannel(Guid id, ClientMessageFragment message)
        {
            // Use lastMessageId to filter out fragments from an already delivered message,
            // while using lastDeliveredFragment (a sortable GUID) for ordering and de-duping.
            if (id == conversationId 
                && message.Id != lastMessageId 
                && message.FragmentId > lastDeliveredFragment)
            {
                lastDeliveredFragment = message.FragmentId;
                channel.Writer.TryWrite(message);
            }
        }

        lock (_eventLock)
        {
            OnMessage += WriteToChannel;
        }

        try
        {
            if (_cache.TryGetValue(conversationId, out var backlog))
            {
                lock (backlog)
                {
                    foreach (var m in backlog)
                    {
                        if (m.Id != lastMessageId && m.FragmentId > lastDeliveredFragment)
                        {
                            lastDeliveredFragment = m.FragmentId;
                            channel.Writer.TryWrite(m);
                        }
                    }
                }
            }

            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return message;
            }
        }
        finally
        {
            lock (_eventLock)
            {
                OnMessage -= WriteToChannel;
            }
        }
    }

    public void Dispose()
    {
        foreach (var (_, semaphore) in _streamLocks)
        {
            semaphore.Dispose();
        }
        _streamLocks.Clear();
        _cache.Clear();
    }
}

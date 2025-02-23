using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

public class InMemoryConversationState : IConversationState, IDisposable
{
    private readonly ConcurrentDictionary<Guid, ConversationState> _conversationState = [];
    private readonly Lock _eventLock = new();
    private Action<Guid, ClientMessageFragment>? OnNewFragment;

    public Task PublishFragmentAsync(Guid conversationId, ClientMessageFragment fragment)
    {
        var state = _conversationState.GetOrAdd(conversationId, _ => new ConversationState());
        var list = state.Backlog;

        lock (list)
        {
            list.Add(fragment);
        }

        // Fire event immediately after publishing fragment
        NotifySubscribers(conversationId, fragment);
        return Task.CompletedTask;
    }

    private void NotifySubscribers(Guid conversationId, ClientMessageFragment fragment)
    {
        Action<Guid, ClientMessageFragment>? handlers;
        lock (_eventLock)
        {
            handlers = OnNewFragment;
        }
        handlers?.Invoke(conversationId, fragment);
    }

    public async IAsyncEnumerable<ClientMessageFragment> Subscribe(Guid conversationId, Guid? lastMessageId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<ClientMessageFragment>();

        // Resolve conversation state.
        _conversationState.TryGetValue(conversationId, out var convState);

        void WriteToChannel(Guid id, ClientMessageFragment message)
        {
            if (id != conversationId)
            {
                return;
            }

            if (lastMessageId is not null && message.Id <= lastMessageId)
            {
                return;
            }

            channel.Writer.TryWrite(message);
        }

        lock (_eventLock)
        {
            OnNewFragment += WriteToChannel;
        }

        try
        {
            if (convState is not null)
            {
                lock (convState.Backlog)
                {
                    foreach (var m in convState.Backlog)
                    {
                        WriteToChannel(conversationId, m);
                    }
                }
            }

            using var reg = cancellationToken.Register(() => channel.Writer.TryComplete());

            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return message;
            }
        }
        finally
        {
            lock (_eventLock)
            {
                OnNewFragment -= WriteToChannel;
            }
        }
    }

    public void Dispose()
    {
        foreach (var (_, state) in _conversationState)
        {
            state.Semaphore.Dispose();
        }
        _conversationState.Clear();
    }

    public Task CompleteAsync(Guid conversationId, Guid messageId)
    {
        _conversationState.TryGetValue(conversationId, out var state);

        if (state is not null)
        {
            lock (state.Backlog)
            {
                state.Backlog.RemoveAll(m => m.Id == messageId);
            }
        }

        return Task.CompletedTask;
    }

    private sealed class ConversationState
    {
        public List<ClientMessageFragment> Backlog { get; } = [];
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
    }
}
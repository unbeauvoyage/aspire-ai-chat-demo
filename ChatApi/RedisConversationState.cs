using System.Text.Json;
using StackExchange.Redis;
using System.Threading.Channels;
using System.Runtime.CompilerServices;

public class RedisConversationState(IConnectionMultiplexer redis) : IConversationState
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly ISubscriber _subscriber = redis.GetSubscriber();

    public async Task PublishFragmentAsync(Guid conversationId, ClientMessageFragment fragment)
    {
        var key = GetBacklogKey(conversationId);
        string serialized = JsonSerializer.Serialize(fragment);
        await _db.ListRightPushAsync(key, serialized);

        var channel = GetRedisChannelName(conversationId);
        await _subscriber.PublishAsync(channel, serialized);
    }

    private async Task<List<ClientMessageFragment>> GetBacklogAsync(Guid conversationId)
    {
        var key = GetBacklogKey(conversationId);
        var values = await _db.ListRangeAsync(key);
        return values.Select(v => JsonSerializer.Deserialize<ClientMessageFragment>(v!))
                     .Where(f => f is not null)
                     .Cast<ClientMessageFragment>()
                     .ToList();
    }

    public async IAsyncEnumerable<ClientMessageFragment> Subscribe(Guid conversationId, Guid? lastMessageId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<ClientMessageFragment>();

        var backlog = await GetBacklogAsync(conversationId);
        foreach (var fragment in backlog)
        {
            yield return fragment;
        }

        var channelName = GetRedisChannelName(conversationId);

        void WriteToChannel(Guid id, ClientMessageFragment message)
        {
            if (id != conversationId)
            {
                return;
            }

            channel.Writer.TryWrite(message);
        }

        _subscriber.Subscribe(channelName, (ch, value) =>
        {
            var fragment = JsonSerializer.Deserialize<ClientMessageFragment>((byte[])value!);
            if (fragment is not null)
            {
                WriteToChannel(conversationId, fragment);
            }
        });

        try
        {
            using var reg = cancellationToken.Register(() => channel.Writer.TryComplete());

            await foreach (var fragment in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return fragment;
            }
        }
        finally
        {
            _subscriber.Unsubscribe(channelName);
        }
    }

    public Task CompleteAsync(Guid conversationId, Guid messageId)
    {
        // TODO: Implement completion logic
        return Task.CompletedTask;
    }

    private static RedisKey GetBacklogKey(Guid conversationId) => $"conversation:{conversationId}:backlog";
    private static RedisChannel GetRedisChannelName(Guid conversationId) => RedisChannel.Literal($"conversation:{conversationId}:channel");
}

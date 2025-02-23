using System.Collections.Concurrent;
using StackExchange.Redis;

public class RedisCancellationManager : ICancellationManager, IDisposable
{
    private readonly ISubscriber _subscriber;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _tokens = [];
    private readonly RedisChannel _channelName = RedisChannel.Literal("chatapp-cancellation");

    public RedisCancellationManager(IConnectionMultiplexer redis)
    {
        _subscriber = redis.GetSubscriber();
        _subscriber.Subscribe(_channelName, (channel, message) =>
        {
            if (Guid.TryParse(message, out Guid replyId) && _tokens.TryRemove(replyId, out var cts))
            {
                cts.Cancel();
            }
        });
    }

    public CancellationToken GetCancellationToken(Guid assistantReplyId)
    {
        var cts = new CancellationTokenSource();
        // Register this token source so that it can be cancelled if a message is published.
        _tokens[assistantReplyId] = cts;
        return cts.Token;
    }

    public async Task CancelAsync(Guid assistantReplyId)
    {
        // publish a cancellation message to Redis.
        await _subscriber.PublishAsync(_channelName, assistantReplyId.ToString());
    }

    public void Dispose()
    {
        foreach (var kvp in _tokens)
        {
            kvp.Value.Dispose();
        }
    }
}

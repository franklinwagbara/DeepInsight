using System.Text.Json;
using DeepInsight.Core.Interfaces;
using DeepInsight.Core.Models;
using StackExchange.Redis;

namespace DeepInsight.Infrastructure.Queue;

public class RedisStreamPublisher : IEventQueuePublisher
{
    private readonly IConnectionMultiplexer _redis;
    private const string StreamKey = "deep-insight:events";

    public RedisStreamPublisher(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task PublishAsync(EventBatch batch, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var payload = JsonSerializer.Serialize(batch);

        await db.StreamAddAsync(
            StreamKey,
            new NameValueEntry[]
            {
                new("data", payload),
                new("project_id", batch.ProjectId),
                new("session_id", batch.SessionId),
                new("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())
            },
            maxLength: 1_000_000, // Cap stream size
            useApproximateMaxLength: true
        );
    }
}

using System.Text.Json;
using DeepInsight.Core.Interfaces;
using DeepInsight.Core.Models;
using StackExchange.Redis;

namespace DeepInsight.Infrastructure.Queue;

public class RedisStreamConsumer : IEventQueueConsumer
{
    private readonly IConnectionMultiplexer _redis;
    private const string StreamKey = "deep-insight:events";
    private const string GroupName = "event-processors";
    private readonly string _consumerName;

    public RedisStreamConsumer(IConnectionMultiplexer redis, string? consumerName = null)
    {
        _redis = redis;
        _consumerName = consumerName ?? $"consumer-{Environment.MachineName}-{Environment.ProcessId}";
    }

    public async Task EnsureConsumerGroupAsync()
    {
        var db = _redis.GetDatabase();
        try
        {
            await db.StreamCreateConsumerGroupAsync(StreamKey, GroupName, "0-0", createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists
        }
    }

    public async Task<IReadOnlyList<EventBatch>> ConsumeAsync(int maxCount, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var entries = await db.StreamReadGroupAsync(
            StreamKey,
            GroupName,
            _consumerName,
            ">",
            maxCount
        );

        if (entries == null || entries.Length == 0)
            return Array.Empty<EventBatch>();

        var batches = new List<EventBatch>();
        foreach (var entry in entries)
        {
            var data = entry["data"];
            if (data.HasValue)
            {
                try
                {
                    var batch = JsonSerializer.Deserialize<EventBatch>(data!);
                    if (batch != null)
                    {
                        batches.Add(batch);
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed entries, acknowledge to avoid reprocessing
                    await db.StreamAcknowledgeAsync(StreamKey, GroupName, entry.Id);
                }
            }
        }
        return batches;
    }

    public async Task AcknowledgeAsync(string messageId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.StreamAcknowledgeAsync(StreamKey, GroupName, messageId);
    }

    public async Task AcknowledgeBatchAsync(IEnumerable<string> messageIds, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var ids = messageIds.Select(id => (RedisValue)id).ToArray();
        await db.StreamAcknowledgeAsync(StreamKey, GroupName, ids);
    }
}

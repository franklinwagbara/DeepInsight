using DeepInsight.Core.Models;

namespace DeepInsight.Core.Interfaces;

public interface IEventQueuePublisher
{
    Task PublishAsync(EventBatch batch, CancellationToken ct = default);
}

public interface IEventQueueConsumer
{
    Task<IReadOnlyList<EventBatch>> ConsumeAsync(int maxCount, CancellationToken ct = default);
    Task AcknowledgeAsync(string messageId, CancellationToken ct = default);
}

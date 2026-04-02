using DeepInsight.Core.Models;

namespace DeepInsight.Core.Interfaces;

public interface IEventStore
{
    Task InsertEventsAsync(IEnumerable<TrackingEvent> events, CancellationToken ct = default);
    Task<IReadOnlyList<TrackingEvent>> GetSessionEventsAsync(Guid projectId, string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<HeatmapData>> GetClickHeatmapAsync(Guid projectId, string pageUrl, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<HeatmapData>> GetScrollHeatmapAsync(Guid projectId, string pageUrl, DateTime from, DateTime to, CancellationToken ct = default);
    Task InsertHeatmapDataAsync(IEnumerable<HeatmapData> data, CancellationToken ct = default);
    Task<long> GetTotalEventsAsync(Guid projectId, DateTime from, DateTime to, CancellationToken ct = default);
}

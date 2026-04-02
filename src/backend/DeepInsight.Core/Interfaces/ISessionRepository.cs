using DeepInsight.Core.Models;

namespace DeepInsight.Core.Interfaces;

public interface ISessionRepository
{
    Task<Session?> GetBySessionIdAsync(Guid projectId, string sessionId, CancellationToken ct = default);
    Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Session> Items, int TotalCount)> GetSessionsAsync(SessionFilter filter, CancellationToken ct = default);
    Task UpsertAsync(Session session, CancellationToken ct = default);
    Task UpdateFrustrationFlagsAsync(Guid id, bool rageClicks, bool deadClicks, bool quickBacks, bool excessiveScrolling, CancellationToken ct = default);
}

public class SessionFilter
{
    public Guid ProjectId { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? Country { get; set; }
    public int? MinDurationMs { get; set; }
    public int? MaxDurationMs { get; set; }
    public bool? HasRageClicks { get; set; }
    public bool? HasDeadClicks { get; set; }
    public string? PageUrl { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

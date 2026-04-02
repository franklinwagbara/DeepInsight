namespace DeepInsight.Core.Models;

public class Session
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? IpAnonymized { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public long DurationMs { get; set; }
    public int PageCount { get; set; }
    public int EventCount { get; set; }
    public bool HasRageClicks { get; set; }
    public bool HasDeadClicks { get; set; }
    public bool HasQuickBacks { get; set; }
    public bool HasExcessiveScrolling { get; set; }
    public bool IsProcessed { get; set; }
    public string? UserId { get; set; } // Developer's user ID via identify()

    public Project? Project { get; set; }
    public List<PageVisit> Pages { get; set; } = new();
}

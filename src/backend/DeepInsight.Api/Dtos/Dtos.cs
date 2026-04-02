using System.ComponentModel.DataAnnotations;

namespace DeepInsight.Api.Dtos;

// Auth
public record RegisterRequest(
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MinLength(8)][MaxLength(100)] string Password,
    [Required][MaxLength(200)] string Name
);

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(string Token, string Email, string Name, DateTime ExpiresAt);

// Projects
public record CreateProjectRequest(
    [Required][MaxLength(200)] string Name,
    [MaxLength(500)] string? Domain
);

public record ProjectResponse(
    Guid Id,
    string Name,
    string Domain,
    Guid TrackingId,
    DateTime CreatedAt,
    bool IsActive
);

// Sessions
public record SessionResponse(
    Guid Id,
    string SessionId,
    string? DeviceType,
    string? Browser,
    string? Os,
    string? Country,
    string? City,
    DateTime StartedAt,
    DateTime? EndedAt,
    long DurationMs,
    int PageCount,
    int EventCount,
    bool HasRageClicks,
    bool HasDeadClicks,
    bool HasQuickBacks,
    bool HasExcessiveScrolling,
    List<PageVisitResponse> Pages
);

public record PageVisitResponse(string Url, string? Title, DateTime EnteredAt, DateTime? LeftAt);

public record SessionListResponse(
    List<SessionResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

// Analytics
public record AnalyticsOverviewResponse(
    int TotalSessions,
    int ActiveUsers,
    double AvgSessionDurationMs,
    double BounceRate,
    long TotalEvents,
    int FrustrationSessions
);

// Heatmap
public record HeatmapPointResponse(float XPct, float YPct, int Count);

public record HeatmapResponse(
    string PageUrl,
    string Type,
    List<HeatmapPointResponse> Points
);

// Scroll depth
public record ScrollDepthResponse(int DepthPct, int SessionCount);

using DeepInsight.Core.Interfaces;
using DeepInsight.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DeepInsight.Infrastructure.Persistence;

public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _db;

    public SessionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Session?> GetBySessionIdAsync(Guid projectId, string sessionId, CancellationToken ct = default)
    {
        return await _db.Sessions
            .Include(s => s.Pages)
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.SessionId == sessionId, ct);
    }

    public async Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Sessions
            .Include(s => s.Pages)
            .Include(s => s.Project)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<(IReadOnlyList<Session> Items, int TotalCount)> GetSessionsAsync(SessionFilter filter, CancellationToken ct = default)
    {
        var query = _db.Sessions
            .Where(s => s.ProjectId == filter.ProjectId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.DeviceType))
            query = query.Where(s => s.DeviceType == filter.DeviceType);

        if (!string.IsNullOrEmpty(filter.Browser))
            query = query.Where(s => s.Browser != null && s.Browser.Contains(filter.Browser));

        if (!string.IsNullOrEmpty(filter.Country))
            query = query.Where(s => s.Country == filter.Country);

        if (filter.MinDurationMs.HasValue)
            query = query.Where(s => s.DurationMs >= filter.MinDurationMs.Value);

        if (filter.MaxDurationMs.HasValue)
            query = query.Where(s => s.DurationMs <= filter.MaxDurationMs.Value);

        if (filter.HasRageClicks.HasValue)
            query = query.Where(s => s.HasRageClicks == filter.HasRageClicks.Value);

        if (filter.HasDeadClicks.HasValue)
            query = query.Where(s => s.HasDeadClicks == filter.HasDeadClicks.Value);

        if (filter.From.HasValue)
            query = query.Where(s => s.StartedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(s => s.StartedAt <= filter.To.Value);

        if (!string.IsNullOrEmpty(filter.PageUrl))
            query = query.Where(s => s.Pages.Any(p => p.Url.Contains(filter.PageUrl)));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.StartedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(s => s.Pages)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task UpsertAsync(Session session, CancellationToken ct = default)
    {
        var existing = await _db.Sessions
            .FirstOrDefaultAsync(s => s.ProjectId == session.ProjectId && s.SessionId == session.SessionId, ct);

        if (existing == null)
        {
            _db.Sessions.Add(session);
        }
        else
        {
            existing.EndedAt = session.EndedAt;
            existing.DurationMs = session.DurationMs;
            existing.PageCount = session.PageCount;
            existing.EventCount = session.EventCount;
            existing.DeviceType = session.DeviceType ?? existing.DeviceType;
            existing.Browser = session.Browser ?? existing.Browser;
            existing.Os = session.Os ?? existing.Os;
            existing.Country = session.Country ?? existing.Country;
            existing.City = session.City ?? existing.City;
            existing.HasRageClicks = session.HasRageClicks || existing.HasRageClicks;
            existing.HasDeadClicks = session.HasDeadClicks || existing.HasDeadClicks;
            existing.HasQuickBacks = session.HasQuickBacks || existing.HasQuickBacks;
            existing.HasExcessiveScrolling = session.HasExcessiveScrolling || existing.HasExcessiveScrolling;
            existing.IsProcessed = session.IsProcessed;
            existing.UserId = session.UserId ?? existing.UserId;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateFrustrationFlagsAsync(Guid id, bool rageClicks, bool deadClicks, bool quickBacks, bool excessiveScrolling, CancellationToken ct = default)
    {
        await _db.Sessions
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.HasRageClicks, rageClicks)
                .SetProperty(x => x.HasDeadClicks, deadClicks)
                .SetProperty(x => x.HasQuickBacks, quickBacks)
                .SetProperty(x => x.HasExcessiveScrolling, excessiveScrolling),
            ct);
    }
}

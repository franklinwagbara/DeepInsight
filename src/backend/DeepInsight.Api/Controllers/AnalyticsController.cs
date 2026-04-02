using System.Security.Claims;
using DeepInsight.Api.Dtos;
using DeepInsight.Core.Interfaces;
using DeepInsight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeepInsight.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEventStore _eventStore;

    public AnalyticsController(AppDbContext db, IEventStore eventStore)
    {
        _db = db;
        _eventStore = eventStore;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AnalyticsOverviewResponse>> GetOverview(
        [FromQuery] Guid projectId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userId = GetUserId();
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null) return NotFound();

        var fromDate = from.HasValue ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc) : DateTime.UtcNow.AddDays(-7);
        var toDate = to.HasValue ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc) : DateTime.UtcNow;

        var sessions = _db.Sessions
            .Where(s => s.ProjectId == projectId && s.StartedAt >= fromDate && s.StartedAt <= toDate);

        var totalSessions = await sessions.CountAsync();
        var avgDuration = totalSessions > 0 ? await sessions.AverageAsync(s => (double)s.DurationMs) : 0;
        var bounceCount = await sessions.CountAsync(s => s.PageCount <= 1);
        var bounceRate = totalSessions > 0 ? (double)bounceCount / totalSessions : 0;
        var activeUsers = await sessions.Select(s => s.UserId).Where(u => u != null).Distinct().CountAsync();
        var frustrationSessions = await sessions.CountAsync(s => s.HasRageClicks || s.HasDeadClicks || s.HasQuickBacks);
        var totalEvents = await _eventStore.GetTotalEventsAsync(projectId, fromDate, toDate);

        return Ok(new AnalyticsOverviewResponse(
            totalSessions,
            activeUsers,
            Math.Round(avgDuration, 0),
            Math.Round(bounceRate * 100, 1),
            totalEvents,
            frustrationSessions
        ));
    }

    [HttpGet("scroll-depth")]
    public async Task<ActionResult<List<ScrollDepthResponse>>> GetScrollDepth(
        [FromQuery] Guid projectId,
        [FromQuery] string pageUrl,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userId = GetUserId();
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null) return NotFound();

        var fromDate = from.HasValue ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc) : DateTime.UtcNow.AddDays(-7);
        var toDate = to.HasValue ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc) : DateTime.UtcNow;

        var data = await _eventStore.GetScrollHeatmapAsync(projectId, pageUrl, fromDate, toDate);

        var depths = data
            .GroupBy(d => (int)Math.Round(d.YPct * 100))
            .Select(g => new ScrollDepthResponse(g.Key, g.Sum(x => x.Count)))
            .OrderBy(x => x.DepthPct)
            .ToList();

        return Ok(depths);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(claim!);
    }
}

using System.Security.Claims;
using DeepInsight.Api.Dtos;
using DeepInsight.Core.Interfaces;
using DeepInsight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeepInsight.Api.Controllers;

[ApiController]
[Route("api/v1/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionRepository _sessions;
    private readonly IEventStore _eventStore;
    private readonly AppDbContext _db;

    public SessionsController(ISessionRepository sessions, IEventStore eventStore, AppDbContext db)
    {
        _sessions = sessions;
        _eventStore = eventStore;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<SessionListResponse>> GetSessions(
        [FromQuery] Guid projectId,
        [FromQuery] string? deviceType,
        [FromQuery] string? browser,
        [FromQuery] string? country,
        [FromQuery] int? minDuration,
        [FromQuery] int? maxDuration,
        [FromQuery] bool? hasRageClicks,
        [FromQuery] bool? hasDeadClicks,
        [FromQuery] string? pageUrl,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null) return NotFound(new { message = "Project not found" });

        pageSize = Math.Clamp(pageSize, 1, 100);

        var filter = new SessionFilter
        {
            ProjectId = projectId,
            DeviceType = deviceType,
            Browser = browser,
            Country = country,
            MinDurationMs = minDuration,
            MaxDurationMs = maxDuration,
            HasRageClicks = hasRageClicks,
            HasDeadClicks = hasDeadClicks,
            PageUrl = pageUrl,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize
        };

        var (items, totalCount) = await _sessions.GetSessionsAsync(filter);

        var response = new SessionListResponse(
            items.Select(s => MapSession(s)).ToList(),
            totalCount,
            page,
            pageSize
        );

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionResponse>> GetSession(Guid id)
    {
        var session = await _sessions.GetByIdAsync(id);
        if (session == null) return NotFound();

        var userId = GetUserId();
        if (session.Project?.UserId != userId) return NotFound();

        return Ok(MapSession(session));
    }

    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult> GetSessionEvents(Guid id)
    {
        var session = await _sessions.GetByIdAsync(id);
        if (session == null) return NotFound();

        var userId = GetUserId();
        if (session.Project?.UserId != userId) return NotFound();

        var events = await _eventStore.GetSessionEventsAsync(session.ProjectId, session.SessionId);

        return Ok(events.Select(e => new
        {
            e.Timestamp,
            e.Type,
            e.PageUrl,
            e.Data
        }));
    }

    private static SessionResponse MapSession(Core.Models.Session s) => new(
        s.Id,
        s.SessionId,
        s.DeviceType,
        s.Browser,
        s.Os,
        s.Country,
        s.City,
        s.StartedAt,
        s.EndedAt,
        s.DurationMs,
        s.PageCount,
        s.EventCount,
        s.HasRageClicks,
        s.HasDeadClicks,
        s.HasQuickBacks,
        s.HasExcessiveScrolling,
        s.Pages.Select(p => new PageVisitResponse(p.Url, p.Title, p.EnteredAt, p.LeftAt)).ToList()
    );

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(claim!);
    }
}

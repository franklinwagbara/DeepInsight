using System.Security.Claims;
using DeepInsight.Api.Dtos;
using DeepInsight.Core.Interfaces;
using DeepInsight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeepInsight.Api.Controllers;

[ApiController]
[Route("api/v1/heatmaps")]
[Authorize]
public class HeatmapsController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly AppDbContext _db;

    public HeatmapsController(IEventStore eventStore, AppDbContext db)
    {
        _eventStore = eventStore;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<HeatmapResponse>> GetHeatmap(
        [FromQuery] Guid projectId,
        [FromQuery] string pageUrl,
        [FromQuery] string type = "click",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userId = GetUserId();
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null) return NotFound();

        var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
        var toDate = to ?? DateTime.UtcNow;

        var data = type == "scroll"
            ? await _eventStore.GetScrollHeatmapAsync(projectId, pageUrl, fromDate, toDate)
            : await _eventStore.GetClickHeatmapAsync(projectId, pageUrl, fromDate, toDate);

        var points = data.Select(d => new HeatmapPointResponse(d.XPct, d.YPct, d.Count)).ToList();

        return Ok(new HeatmapResponse(pageUrl, type, points));
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(claim!);
    }
}

using System.Security.Claims;
using DeepInsight.Api.Dtos;
using DeepInsight.Core.Models;
using DeepInsight.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeepInsight.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProjectsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> GetProjects()
    {
        var userId = GetUserId();
        var projects = await _db.Projects
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectResponse(
                p.Id, p.Name, p.Domain, p.TrackingId, p.CreatedAt, p.IsActive
            ))
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> GetProject(Guid id)
    {
        var userId = GetUserId();
        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (project == null) return NotFound();

        return Ok(new ProjectResponse(
            project.Id, project.Name, project.Domain, project.TrackingId, project.CreatedAt, project.IsActive
        ));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> CreateProject([FromBody] CreateProjectRequest request)
    {
        var userId = GetUserId();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Domain = request.Domain ?? string.Empty,
            TrackingId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProject), new { id = project.Id },
            new ProjectResponse(project.Id, project.Name, project.Domain, project.TrackingId, project.CreatedAt, project.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateProject(Guid id, [FromBody] CreateProjectRequest request)
    {
        var userId = GetUserId();
        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (project == null) return NotFound();

        project.Name = request.Name;
        project.Domain = request.Domain ?? project.Domain;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProject(Guid id)
    {
        var userId = GetUserId();
        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (project == null) return NotFound();

        project.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(claim!);
    }
}

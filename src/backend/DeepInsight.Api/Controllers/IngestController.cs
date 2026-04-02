using DeepInsight.Core.Interfaces;
using DeepInsight.Core.Models;
using DeepInsight.Infrastructure.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace DeepInsight.Api.Controllers;

[ApiController]
[Route("api/v1/ingest")]
[EnableCors("AllowSDK")]
public class IngestController : ControllerBase
{
    private readonly IEventQueuePublisher _publisher;
    private readonly ILogger<IngestController> _logger;

    public IngestController(IEventQueuePublisher publisher, ILogger<IngestController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(1_048_576)] // 1MB max
    public async Task<IActionResult> Ingest([FromBody] EventBatch batch)
    {
        if (string.IsNullOrEmpty(batch.ProjectId) || string.IsNullOrEmpty(batch.SessionId))
            return BadRequest(new { message = "projectId and sessionId are required" });

        if (batch.Events == null || batch.Events.Count == 0)
            return BadRequest(new { message = "No events in batch" });

        if (batch.Events.Count > 500)
            return BadRequest(new { message = "Batch too large. Max 500 events." });

        // Anonymize IP before any processing
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var anonymizedIp = GeoLookupService.AnonymizeIp(clientIp);

        // Enrich batch with server-side data
        foreach (var evt in batch.Events)
        {
            if (evt.Timestamp <= 0)
                evt.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        await _publisher.PublishAsync(batch);

        return Accepted();
    }

    [HttpPost("beacon")]
    [RequestSizeLimit(524_288)] // 512KB for beacon
    public async Task<IActionResult> Beacon([FromBody] EventBatch batch)
    {
        // Same as Ingest but always returns 202 for beacon reliability
        if (string.IsNullOrEmpty(batch.ProjectId) || string.IsNullOrEmpty(batch.SessionId))
            return Accepted();

        if (batch.Events == null || batch.Events.Count == 0)
            return Accepted();

        try
        {
            await _publisher.PublishAsync(batch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish beacon batch");
        }

        return Accepted();
    }
}

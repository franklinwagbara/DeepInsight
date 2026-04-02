using System.Text.Json;
using DeepInsight.Core.Interfaces;
using DeepInsight.Core.Models;
using DeepInsight.Infrastructure.Queue;
using DeepInsight.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeepInsight.Processing.Workers;

public class EventConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly RedisStreamConsumer _consumer;
    private readonly ILogger<EventConsumerWorker> _logger;

    public EventConsumerWorker(
        IServiceProvider services,
        RedisStreamConsumer consumer,
        ILogger<EventConsumerWorker> logger)
    {
        _services = services;
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumer.EnsureConsumerGroupAsync();
        _logger.LogInformation("EventConsumerWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batches = await _consumer.ConsumeAsync(100, stoppingToken);

                if (batches.Count == 0)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                using var scope = _services.CreateScope();
                var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
                var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
                var deviceParser = scope.ServiceProvider.GetRequiredService<DeviceParserService>();
                var geoService = scope.ServiceProvider.GetRequiredService<GeoLookupService>();

                foreach (var batch in batches)
                {
                    await ProcessBatchAsync(batch, eventStore, sessionRepo, deviceParser, geoService, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event batch");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task ProcessBatchAsync(
        EventBatch batch,
        IEventStore eventStore,
        ISessionRepository sessionRepo,
        DeviceParserService deviceParser,
        GeoLookupService geoService,
        CancellationToken ct)
    {
        if (!Guid.TryParse(batch.ProjectId, out var projectId))
        {
            _logger.LogWarning("Invalid project ID: {ProjectId}", batch.ProjectId);
            return;
        }

        // Convert and store raw events
        var trackingEvents = batch.Events.Select(e => new TrackingEvent
        {
            ProjectId = projectId,
            SessionId = batch.SessionId,
            Timestamp = e.Timestamp,
            Type = e.Type,
            PageUrl = e.PageUrl,
            Data = e.Data
        }).ToList();

        await eventStore.InsertEventsAsync(trackingEvents, ct);

        // Upsert session metadata
        var session = await sessionRepo.GetBySessionIdAsync(projectId, batch.SessionId, ct);
        var isNew = session == null;

        if (isNew)
        {
            session = new Session
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                SessionId = batch.SessionId,
                StartedAt = DateTimeOffset.FromUnixTimeMilliseconds(
                    batch.Events.Min(e => e.Timestamp)).UtcDateTime,
            };
        }

        var lastTimestamp = batch.Events.Max(e => e.Timestamp);
        session!.EndedAt = DateTimeOffset.FromUnixTimeMilliseconds(lastTimestamp).UtcDateTime;
        session.DurationMs = (long)(session.EndedAt.Value - session.StartedAt).TotalMilliseconds;
        session.EventCount += batch.Events.Count;

        // Count page views
        var pageViews = batch.Events.Where(e => e.Type == "pageview").ToList();
        session.PageCount += pageViews.Count;

        // Frustration detection
        var rageClicks = batch.Events.Any(e => e.Type == "rage_click");
        var deadClicks = batch.Events.Any(e => e.Type == "dead_click");
        if (rageClicks) session.HasRageClicks = true;
        if (deadClicks) session.HasDeadClicks = true;

        // Quick back detection: pages visited < 3 seconds
        DetectQuickBacks(batch, session);

        // Extract user identity
        var identifyEvent = batch.Events.FirstOrDefault(e => e.Type == "identify");
        if (identifyEvent != null && identifyEvent.Data.TryGetProperty("userId", out var userIdProp))
        {
            session.UserId = userIdProp.GetString();
        }

        await sessionRepo.UpsertAsync(session, ct);

        // Extract heatmap data
        await ExtractHeatmapData(batch, projectId, eventStore, ct);
    }

    private static void DetectQuickBacks(EventBatch batch, Session session)
    {
        var navigations = batch.Events
            .Where(e => e.Type == "navigation")
            .OrderBy(e => e.Timestamp)
            .ToList();

        for (int i = 1; i < navigations.Count; i++)
        {
            var timeDiff = navigations[i].Timestamp - navigations[i - 1].Timestamp;
            if (timeDiff < 3000) // Less than 3 seconds
            {
                session.HasQuickBacks = true;
                break;
            }
        }
    }

    private async Task ExtractHeatmapData(EventBatch batch, Guid projectId, IEventStore eventStore, CancellationToken ct)
    {
        var heatmapEntries = new List<HeatmapData>();

        foreach (var evt in batch.Events.Where(e => e.Type == "click"))
        {
            if (evt.Data.TryGetProperty("xPct", out var xPct) &&
                evt.Data.TryGetProperty("yPct", out var yPct))
            {
                heatmapEntries.Add(new HeatmapData
                {
                    ProjectId = projectId,
                    PageUrl = evt.PageUrl,
                    HeatmapType = "click",
                    XPct = (float)Math.Round(xPct.GetDouble(), 3),
                    YPct = (float)Math.Round(yPct.GetDouble(), 3),
                    Count = 1,
                    Date = DateTime.UtcNow.Date
                });
            }
        }

        foreach (var evt in batch.Events.Where(e => e.Type == "scroll"))
        {
            if (evt.Data.TryGetProperty("depthPct", out var depthPct))
            {
                var depth = (float)Math.Round(depthPct.GetDouble() / 10.0, 0) * 10; // Bucket by 10%
                heatmapEntries.Add(new HeatmapData
                {
                    ProjectId = projectId,
                    PageUrl = evt.PageUrl,
                    HeatmapType = "scroll",
                    XPct = 0,
                    YPct = depth / 100f,
                    Count = 1,
                    Date = DateTime.UtcNow.Date
                });
            }
        }

        if (heatmapEntries.Count > 0)
        {
            await eventStore.InsertHeatmapDataAsync(heatmapEntries, ct);
        }
    }
}

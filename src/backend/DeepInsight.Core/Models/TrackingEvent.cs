using System.Text.Json;

namespace DeepInsight.Core.Models;

public class TrackingEvent
{
    public Guid ProjectId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public JsonElement Data { get; set; }
}

public class EventBatch
{
    public string ProjectId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public List<IncomingEvent> Events { get; set; } = new();
    public long SentAt { get; set; }
    public string SdkVersion { get; set; } = string.Empty;
}

public class IncomingEvent
{
    public string SessionId { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public JsonElement Data { get; set; }
}

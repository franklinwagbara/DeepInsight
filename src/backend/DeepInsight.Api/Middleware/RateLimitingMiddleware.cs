using System.Collections.Concurrent;

namespace DeepInsight.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, RateLimitEntry> _clients = new();
    private readonly int _maxRequests;
    private readonly TimeSpan _window;

    public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 100, int windowSeconds = 60)
    {
        _next = next;
        _maxRequests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only rate limit ingestion endpoints
        if (!context.Request.Path.StartsWithSegments("/api/v1/ingest"))
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var projectId = context.Request.Headers["X-DI-Project"].FirstOrDefault() ?? "unknown";
        var key = $"{clientIp}:{projectId}";

        var now = DateTime.UtcNow;
        var entry = _clients.GetOrAdd(key, _ => new RateLimitEntry { Count = 0, WindowStart = now });

        if (now - entry.WindowStart > _window)
        {
            entry.Count = 0;
            entry.WindowStart = now;
        }

        entry.Count++;

        if (entry.Count > _maxRequests)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = ((int)(_window - (now - entry.WindowStart)).TotalSeconds).ToString();
            await context.Response.WriteAsJsonAsync(new { message = "Rate limit exceeded" });
            return;
        }

        await _next(context);
    }

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}

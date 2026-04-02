using DeepInsight.Core.Interfaces;
using DeepInsight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeepInsight.Processing.Workers;

public class SessionBuilderWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SessionBuilderWorker> _logger;

    public SessionBuilderWorker(IServiceProvider services, ILogger<SessionBuilderWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionBuilderWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Find sessions that haven't been updated in 5 minutes (likely ended)
                var cutoff = DateTime.UtcNow.AddMinutes(-5);
                var staleSessions = await db.Sessions
                    .Where(s => !s.IsProcessed && s.EndedAt < cutoff)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var session in staleSessions)
                {
                    session.IsProcessed = true;
                    if (session.EndedAt.HasValue)
                    {
                        session.DurationMs = (long)(session.EndedAt.Value - session.StartedAt).TotalMilliseconds;
                    }
                }

                if (staleSessions.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Finalized {Count} sessions", staleSessions.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SessionBuilderWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

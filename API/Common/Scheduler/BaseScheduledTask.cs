using Cronos;
using Microsoft.Extensions.Hosting;

namespace API.Common.Scheduler;

/// <summary>
/// Base class for cron-scheduled background tasks.
/// Override <see cref="ExecuteAsync"/> with the work to perform.
/// The cron expression is read from configuration via the provided config key,
/// with a hard-coded fallback.
/// </summary>
public abstract class BaseScheduledTask : BackgroundService
{
    private readonly string _cronExpression;
    private readonly CronExpression _cron;
    private readonly ILogger _logger;

    protected BaseScheduledTask(string cronExpression, ILogger logger)
    {
        _cronExpression = cronExpression;
        _cron = CronExpression.Parse(cronExpression, CronFormat.Standard);
        _logger = logger;
    }

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled task {TaskName} started with cron '{Cron}'",
            GetType().Name, _cronExpression);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var next = _cron.GetNextOccurrence(now, TimeZoneInfo.Utc);

            if (next is null)
            {
                _logger.LogWarning("Cron expression '{Cron}' has no future occurrences. Stopping.", _cronExpression);
                return;
            }

            var delay = next.Value - now;
            _logger.LogDebug("Next run for {TaskName} at {Next:O} (in {Delay})",
                GetType().Name, next.Value, delay);

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await RunTaskAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled task {TaskName} failed", GetType().Name);
            }
        }
    }

    /// <summary>Override with the task's business logic.</summary>
    protected abstract Task RunTaskAsync(CancellationToken ct);
}

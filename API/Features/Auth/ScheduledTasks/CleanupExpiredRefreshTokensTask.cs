using Microsoft.EntityFrameworkCore;
using API.Common.Scheduler;
using API.Data;
using Cronos;

namespace API.Features.Auth.ScheduledTasks;

/// <summary>
/// Runs daily to remove expired refresh tokens from the database.
/// Configurable via the app setting <c>Scheduler:CleanupRefreshTokens:Cron</c>.
/// </summary>
public class CleanupExpiredRefreshTokensTask(
    IServiceScopeFactory scopeFactory,
    ILogger<CleanupExpiredRefreshTokensTask> logger) : BaseScheduledTask("0 3 * * *", logger)
{
    protected override async Task RunTaskAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow;
        var deleted = await db.RefreshTokens
            .Where(rt => rt.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            logger.LogInformation("Cleaned up {Count} expired refresh tokens", deleted);
    }
}

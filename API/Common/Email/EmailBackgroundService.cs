namespace API.Common.Email;

/// <summary>
/// Background service that consumes email queue entries and sends them via the
/// configured IEmailService provider. This replaces ad-hoc Task.Run fire-and-forget
/// calls with a proper producer/consumer pattern.
/// Matches errandigo's BullMQ NotificationConsumer pattern, but in-process.
/// </summary>
public class EmailBackgroundService(
    IEmailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email background worker started");

        await foreach (var entry in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendAsync(entry.To, entry.Subject, entry.TemplatePath, entry.Variables, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email to {To}: {Subject}", entry.To, entry.Subject);
            }
        }
    }
}

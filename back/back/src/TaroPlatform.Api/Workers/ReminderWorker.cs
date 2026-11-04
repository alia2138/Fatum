using TaroPlatform.Application.Reminders;

namespace TaroPlatform.Api.Workers;

/// <summary>
/// Фонова задача: раз на 5 хвилин шукає консультації, що починаються протягом години,
/// і надсилає нагадування.
/// </summary>
public class ReminderWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ReminderWorker> _logger;

    public ReminderWorker(IServiceScopeFactory scopes, ILogger<ReminderWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<IReminderService>();
                var sent = await reminders.SendDueAsync(stoppingToken);

                if (sent > 0)
                    _logger.LogInformation("Надіслано нагадувань про консультації: {Count}", sent);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Помилка під час надсилання нагадувань");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Infrastructure.Notifications;

/// <summary>
/// Надсилає сповіщення на email і в Telegram. Помилка одного каналу не зупиняє інший
/// і не ламає основну дію (наприклад, підтвердження консультації).
/// </summary>
public class ExternalNotifier : IExternalNotifier
{
    private const string TelegramProvider = "telegram";

    private readonly EmailNotifier _email;
    private readonly TelegramBotNotifier _telegram;
    private readonly NotificationSettings _settings;
    private readonly ILogger<ExternalNotifier> _logger;

    public ExternalNotifier(
        EmailNotifier email,
        TelegramBotNotifier telegram,
        IOptions<NotificationSettings> options,
        ILogger<ExternalNotifier> logger)
    {
        _email = email;
        _telegram = telegram;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(User user, string title, string body, string? link, CancellationToken cancellationToken = default)
    {
        var text = link is null
            ? $"{title}\n{body}"
            : $"{title}\n{body}\n{AbsoluteUrl(link)}";

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                await _email.SendAsync(user.Email, title, text, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Email-сповіщення не надіслано");
            }
        }

        var telegramId = user.ExternalLogins
            .FirstOrDefault(e => e.Provider == TelegramProvider)?.ProviderUserId;

        if (telegramId is not null)
        {
            try
            {
                await _telegram.SendAsync(telegramId, text, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Telegram-сповіщення не надіслано (можливо, користувач не відкривав бота)");
            }
        }
    }

    private string AbsoluteUrl(string link) =>
        link.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? link
            : _settings.PublicBaseUrl.TrimEnd('/') + link;
}

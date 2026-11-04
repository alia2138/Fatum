using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TaroPlatform.Infrastructure.Security;

namespace TaroPlatform.Infrastructure.Notifications;

/// <summary>
/// Надсилання повідомлень через Telegram Bot API. Бот може написати користувачу,
/// тільки якщо той хоча б раз відкрив чат із ботом (натиснув /start).
/// </summary>
public class TelegramBotNotifier
{
    private readonly HttpClient _http;
    private readonly TelegramSettings _settings;

    public TelegramBotNotifier(HttpClient http, IOptions<TelegramSettings> options)
    {
        _http = http;
        _settings = options.Value;
    }

    public async Task SendAsync(string chatId, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BotToken))
            return;

        var body = new { chat_id = chatId, text };
        using var response = await _http.PostAsJsonAsync(
            $"https://api.telegram.org/bot{_settings.BotToken}/sendMessage",
            body,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}

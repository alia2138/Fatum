using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaroPlatform.Application.Admin;
using TaroPlatform.Application.Common.Interfaces;

namespace TaroPlatform.Infrastructure.AI;

/// <summary>
/// Баланс AI. OpenRouter віддає використання і ліміт через API. Для інших провайдерів
/// автоматичного балансу немає, тому повертається посилання на їхню сторінку витрат.
/// </summary>
public class AiBalanceProvider : IAiBalanceProvider
{
    private readonly HttpClient _http;
    private readonly AiSettings _settings;
    private readonly ILogger<AiBalanceProvider> _logger;

    public AiBalanceProvider(HttpClient http, IOptions<AiSettings> options, ILogger<AiBalanceProvider> logger)
    {
        _http = http;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<AiBalanceDto> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
            return new AiBalanceDto("none", false, null, null, null, "AI вимкнено або не задано ключ.");

        if (!_settings.BaseUrl.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase))
            return new AiBalanceDto(
                _settings.BaseUrl,
                false,
                null,
                null,
                null,
                "Автоматичний баланс доступний лише для OpenRouter. Перевір витрати в кабінеті свого провайдера.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _settings.BaseUrl.TrimEnd('/') + "/auth/key");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var data = json.RootElement.GetProperty("data");

            return new AiBalanceDto(
                "OpenRouter",
                true,
                ReadNumber(data, "usage"),
                ReadNumber(data, "limit"),
                ReadNumber(data, "limit_remaining"),
                "Суми в доларах США.");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Не вдалося отримати баланс AI");
            return new AiBalanceDto("OpenRouter", false, null, null, null, "Не вдалося отримати баланс. Спробуй пізніше.");
        }
    }

    private static double? ReadNumber(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;
}

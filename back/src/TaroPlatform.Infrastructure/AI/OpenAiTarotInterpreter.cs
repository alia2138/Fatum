using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TaroPlatform.Application.Tarot;

namespace TaroPlatform.Infrastructure.AI;

/// <summary>
/// Справжнє трактування через мовну модель (OpenAI-сумісний /chat/completions).
/// Промпт формує TarotPromptBuilder, тут лише HTTP-виклик і розбір відповіді.
/// </summary>
public class OpenAiTarotInterpreter : ITarotInterpreter
{
    private readonly HttpClient _http;
    private readonly AiSettings _settings;

    public OpenAiTarotInterpreter(HttpClient http, IOptions<AiSettings> options)
    {
        _http = http;
        _settings = options.Value;
    }

    public async Task<InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default)
    {
        var messages = TarotPromptBuilder.Build(context)
            .Select(m => new { role = m.Role, content = m.Text })
            .ToList();

        var body = new
        {
            model = _settings.Model,
            messages,
            max_tokens = _settings.MaxTokens,
            temperature = 0.8
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var text = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("AI повернув порожню відповідь.");

        return new InterpretationResult(text.Trim(), "ai");
    }
}

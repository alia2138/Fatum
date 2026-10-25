using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaroPlatform.Application.Tarot;
using TaroPlatform.Infrastructure.Tarot;

namespace TaroPlatform.Infrastructure.AI;

/// <summary>
/// Патерн Decorator/Fallback: спершу пробує AI, а якщо ключа немає
/// або сервіс недоступний, повертає заглушку. Гадання не падає через збій AI.
/// </summary>
public class FallbackTarotInterpreter : ITarotInterpreter
{
    private readonly OpenAiTarotInterpreter _ai;
    private readonly StubTarotInterpreter _stub;
    private readonly AiSettings _settings;
    private readonly ILogger<FallbackTarotInterpreter> _logger;

    public FallbackTarotInterpreter(
        OpenAiTarotInterpreter ai,
        StubTarotInterpreter stub,
        IOptions<AiSettings> options,
        ILogger<FallbackTarotInterpreter> logger)
    {
        _ai = ai;
        _stub = stub;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
            return await _stub.InterpretAsync(context, cancellationToken);

        try
        {
            return await _ai.InterpretAsync(context, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AI-трактування недоступне, використано заглушку");
            return await _stub.InterpretAsync(context, cancellationToken);
        }
    }
}

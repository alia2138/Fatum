namespace TaroPlatform.Application.Tarot;

/// <summary>Карта (або точка матриці), підготовлена для трактування.</summary>
public record InterpretedCard(string PositionName, string CardName, bool IsReversed, string Meaning);

/// <summary>
/// Вхідні дані для трактування. History — попередній діалог, FollowUp — нове питання.
/// SystemPrompt і CardsLabel задаються для інших режимів (наприклад, матриця долі):
/// тоді картки подаються як точки без напрямку, а не як розклад Таро.
/// </summary>
public record InterpretationContext(
    string TopicTitle,
    string Background,
    string Question,
    IReadOnlyList<InterpretedCard> Cards,
    string? FollowUp,
    IReadOnlyList<ChatTurn> History,
    string? SystemPrompt = null,
    string? CardsLabel = null);

/// <summary>Результат трактування: текст і джерело ("ai" або "stub").</summary>
public record InterpretationResult(string Text, string Provider);

/// <summary>Стратегія трактування (патерн Strategy): AI, заглушка або їх комбінація.</summary>
public interface ITarotInterpreter
{
    Task<InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default);
}

namespace TaroPlatform.Application.Tarot;

/// <summary>Карта, підготовлена для трактування.</summary>
public record InterpretedCard(string PositionName, string CardName, bool IsReversed, string Meaning);

/// <summary>
/// Вхідні дані для трактування. History — попередній діалог (порожній при першій відповіді),
/// FollowUp — нове додаткове питання (null при першій відповіді).
/// </summary>
public record InterpretationContext(
    string TopicTitle,
    string Background,
    string Question,
    IReadOnlyList<InterpretedCard> Cards,
    string? FollowUp,
    IReadOnlyList<ChatTurn> History);

/// <summary>Результат трактування: текст і джерело ("ai" або "stub").</summary>
public record InterpretationResult(string Text, string Provider);

/// <summary>Стратегія трактування (патерн Strategy): AI, заглушка або їх комбінація.</summary>
public interface ITarotInterpreter
{
    Task<InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default);
}

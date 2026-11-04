using System.Text;

namespace TaroPlatform.Application.Tarot;

/// <summary>
/// Формує повідомлення для мовної моделі. Режим Таро — за замовчуванням.
/// Якщо задано CardsLabel (режим матриці), картки подаються як точки без напрямку.
/// </summary>
public static class TarotPromptBuilder
{
    private const string TarotSystemPrompt =
        "Ти досвідчений тлумач Таро. Говориш українською теплим і спокійним тоном. " +
        "Трактуєш карти з урахуванням теми, передісторії користувача, позицій карт і їхнього положення (пряме або перевернуте). " +
        "Не вигадуй карт, яких немає у вказаних картах. " +
        "Якщо є попередня розмова, це продовження одного гадання: враховуй передісторію, головне питання " +
        "і попередні відповіді, але тлумач саме нові карти, що додані до поточного питання. " +
        "Не повторюй дослівно попередні відповіді. " +
        "Відповідь має бути до 250 слів. Структура: коротко пояснити кожну карту, " +
        "потім відповідь на питання та одна практична порада. " +
        "Карти це спосіб поглянути на ситуацію, а не точне передбачення. " +
        "Питання про здоров'я, гроші чи важливі життєві рішення: нагадай, що такі рішення варто приймати " +
        "з урахуванням фахових порад.";

    public static IReadOnlyList<ChatTurn> Build(InterpretationContext context)
    {
        var messages = new List<ChatTurn>
        {
            new("system", context.SystemPrompt ?? TarotSystemPrompt),
            new("user", BuildCardBlock(context))
        };

        messages.AddRange(context.History);

        if (context.FollowUp is not null)
            messages.Add(new ChatTurn("user", context.FollowUp));

        return messages;
    }

    private static string BuildCardBlock(InterpretationContext context)
    {
        var isFollowUp = context.FollowUp is not null;
        var isPointsMode = context.CardsLabel is not null;
        var sb = new StringBuilder();

        sb.AppendLine($"Тема: {context.TopicTitle}");

        if (!string.IsNullOrWhiteSpace(context.Background))
            sb.AppendLine($"Передісторія користувача: {context.Background}");

        sb.AppendLine($"{(isFollowUp ? "Головне питання" : "Питання")}: {context.Question}");

        if (isPointsMode)
        {
            sb.AppendLine($"{context.CardsLabel}:");
            foreach (var point in context.Cards)
                sb.AppendLine($"- {point.PositionName}: {point.CardName}. Значення: {point.Meaning}");

            return sb.ToString().TrimEnd();
        }

        sb.AppendLine(isFollowUp ? "Нові карти для поточного питання:" : "Карти:");

        foreach (var card in context.Cards)
        {
            var orientation = card.IsReversed ? "перевернута" : "пряма";
            sb.AppendLine($"- {card.PositionName}: {card.CardName} ({orientation}). Значення: {card.Meaning}");
        }

        return sb.ToString().TrimEnd();
    }
}

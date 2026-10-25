using System.Text;

namespace TaroPlatform.Application.Tarot;

/// <summary>
/// Формує повідомлення для мовної моделі. Окремий клас, щоб промпт можна було тестувати
/// і міняти, не торкаючись HTTP-клієнта.
/// </summary>
public static class TarotPromptBuilder
{
    private const string SystemPrompt =
        "Ти досвідчений тлумач Таро. Говориш українською теплим і спокійним тоном. " +
        "Трактуєш розклад з урахуванням теми, передісторії користувача, позицій карт і їхнього положення " +
        "(пряме або перевернуте). Не вигадуй карт, яких немає в розкладі. " +
        "Для першої відповіді: коротко поясни кожну позицію, потім дай загальний висновок і одну практичну пораду. " +
        "Для додаткового питання: відповідай саме на нього, спираючись на той самий розклад. " +
        "Відповідь має бути до 250 слів. Карти це спосіб поглянути на ситуацію, а не точне передбачення. " +
        "Питання про здоров'я, гроші чи важливі життєві рішення: нагадай, що такі рішення варто приймати " +
        "з урахуванням фахових порад.";

    public static IReadOnlyList<ChatTurn> Build(InterpretationContext context)
    {
        var messages = new List<ChatTurn>
        {
            new("system", SystemPrompt),
            new("user", BuildCardBlock(context))
        };

        messages.AddRange(context.History);

        if (context.FollowUp is not null)
            messages.Add(new ChatTurn("user", context.FollowUp));

        return messages;
    }

    private static string BuildCardBlock(InterpretationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Тема: {context.TopicTitle}");
        sb.AppendLine($"Передісторія користувача: {context.Background}");
        sb.AppendLine($"Питання: {context.Question}");
        sb.AppendLine("Карти розкладу:");

        foreach (var card in context.Cards)
        {
            var orientation = card.IsReversed ? "перевернута" : "пряма";
            sb.AppendLine($"- {card.PositionName}: {card.CardName} ({orientation}). Значення: {card.Meaning}");
        }

        return sb.ToString().TrimEnd();
    }
}

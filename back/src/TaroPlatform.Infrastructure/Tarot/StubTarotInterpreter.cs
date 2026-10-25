using System.Text;
using TaroPlatform.Application.Tarot;

namespace TaroPlatform.Infrastructure.Tarot;

/// <summary>
/// Заглушка без AI. Використовується, коли ключ не налаштовано або AI недоступний.
/// </summary>
public class StubTarotInterpreter : ITarotInterpreter
{
    private const string Disclaimer = "(Тимчасова відповідь без AI. Щоб отримати персональне трактування, додайте ключ моделі в налаштування.)";

    public Task<InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        var summary = BuildSummary(context.Cards);

        if (context.FollowUp is null)
        {
            sb.AppendLine($"Тема: {context.TopicTitle}.");
            sb.AppendLine($"Ваша ситуація: {context.Background}");
            sb.AppendLine($"Питання: «{context.Question}»");
            sb.AppendLine();

            foreach (var card in context.Cards)
            {
                var reversedNote = card.IsReversed ? " (перевернута)" : string.Empty;
                sb.AppendLine($"• {card.PositionName}: {card.CardName}{reversedNote}. {card.Meaning}");
            }

            sb.AppendLine();
            sb.Append($"Загальний висновок: {summary}");
        }
        else
        {
            var first = context.Cards.FirstOrDefault();

            sb.AppendLine($"Ви уточнили: «{context.FollowUp}».");
            if (first is not null)
                sb.AppendLine($"Найближча до цього позиція «{first.PositionName}» показує: {first.CardName}. {first.Meaning}");
            sb.Append($"Загальний настрій розкладу: {summary}");
        }

        sb.AppendLine();
        sb.AppendLine();
        sb.Append(Disclaimer);

        return Task.FromResult(new InterpretationResult(sb.ToString(), "stub"));
    }

    private static string BuildSummary(IReadOnlyList<InterpretedCard> cards)
    {
        if (cards.Count == 0)
            return "карт немає.";

        var reversed = cards.Count(c => c.IsReversed);

        if (reversed == 0)
            return "усі карти прямі, енергія розкладу спрямована вперед.";

        if (reversed * 2 > cards.Count)
            return "більшість карт перевернуті, варто звернути увагу на внутрішні зміни та затримки.";

        return "карти змішані: є і підтримка, і внутрішні виклики, їх варто зважити разом.";
    }
}

using TaroPlatform.Domain.Common;

namespace TaroPlatform.Domain.Tarot;

/// <summary>
/// Розклад (сесія гадання). Містить передісторію, питання, карти
/// та діалог із додатковими запитаннями. Усе зберігається в MongoDB.
/// </summary>
public class TarotReading : Entity
{
    protected TarotReading() { }

    public TarotReading(
        string userId,
        ReadingTopic topic,
        string background,
        string question,
        List<DrawnCard> cards)
    {
        UserId = userId;
        Topic = topic;
        Background = background;
        Question = question;
        Cards = cards;
    }

    public string UserId { get; private set; } = string.Empty;
    public ReadingTopic Topic { get; private set; }
    public string Background { get; private set; } = string.Empty;
    public string Question { get; private set; } = string.Empty;
    public List<DrawnCard> Cards { get; private set; } = new();
    public List<ReadingMessage> Messages { get; private set; } = new();

    public void AddMessage(MessageRole role, string text, string provider = "") =>
        Messages.Add(new ReadingMessage(role, text, provider));
}

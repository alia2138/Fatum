namespace TaroPlatform.Domain.Tarot;

/// <summary>
/// Витягнута карта у конкретній позиції розкладу. Зберігається всередині гадання.
/// </summary>
public class DrawnCard
{
    protected DrawnCard() { }

    public DrawnCard(int position, string positionName, int cardId, bool isReversed)
    {
        Position = position;
        PositionName = positionName;
        CardId = cardId;
        IsReversed = isReversed;
    }

    public int Position { get; private set; }
    public string PositionName { get; private set; } = string.Empty;
    public int CardId { get; private set; }
    public bool IsReversed { get; private set; }
}

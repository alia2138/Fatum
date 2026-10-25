namespace TaroPlatform.Domain.Tarot;

/// <summary>
/// Витягнута карта. Round = 0 для карт першого розкладу,
/// 1, 2, ... для карт, витягнутих під час додаткових питань.
/// </summary>
public class DrawnCard
{
    protected DrawnCard() { }

    public DrawnCard(int position, string positionName, int cardId, bool isReversed, int round = 0)
    {
        Position = position;
        PositionName = positionName;
        CardId = cardId;
        IsReversed = isReversed;
        Round = round;
    }

    public int Position { get; private set; }
    public string PositionName { get; private set; } = string.Empty;
    public int CardId { get; private set; }
    public bool IsReversed { get; private set; }
    public int Round { get; private set; }
}

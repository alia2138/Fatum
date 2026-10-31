using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Application.Tarot;

/// <summary>
/// Випадкове витягування карт: для розкладу та для додаткових питань.
/// Карти не повторюються.
/// </summary>
public class CardDrawService
{
    private const int MaxFollowUpCards = 3;

    private readonly IRandomSource _random;

    public CardDrawService(IRandomSource random)
    {
        _random = random;
    }

    /// <summary>Карти першого розкладу: по одній на кожну позицію.</summary>
    public IReadOnlyList<DrawnCard> Draw(SpreadDefinition spread)
    {
        var pool = TarotDeck.All.Select(c => c.Id).ToList();
        var result = new List<DrawnCard>();

        foreach (var position in spread.Positions)
        {
            var cardId = TakeRandom(pool);
            var isReversed = _random.Next(2) == 1;
            result.Add(new DrawnCard(position.Index, position.Name, cardId, isReversed));
        }

        return result;
    }

    /// <summary>
    /// Додаткове витягування: від 1 до 3 карт (кількість випадкова),
    /// без карт, які вже випали в цьому гаданні.
    /// </summary>
    public IReadOnlyList<DrawnCard> DrawFollowUp(IEnumerable<int> usedCardIds, int round)
    {
        var pool = TarotDeck.All.Select(c => c.Id).Except(usedCardIds).ToList();
        var count = _random.Next(MaxFollowUpCards) + 1;
        var result = new List<DrawnCard>();

        for (var i = 0; i < count; i++)
        {
            var cardId = TakeRandom(pool);
            var isReversed = _random.Next(2) == 1;
            result.Add(new DrawnCard(i + 1, $"Уточнення {round}, карта {i + 1}", cardId, isReversed, round));
        }

        return result;
    }

    private int TakeRandom(List<int> pool)
    {
        var index = _random.Next(pool.Count);
        var cardId = pool[index];
        pool.RemoveAt(index);
        return cardId;
    }
}

using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Application.Tarot;

/// <summary>
/// Випадково витягує карти для розкладу: без повторів, з випадковим положенням.
/// </summary>
public class CardDrawService
{
    private readonly IRandomSource _random;

    public CardDrawService(IRandomSource random)
    {
        _random = random;
    }

    public IReadOnlyList<DrawnCard> Draw(SpreadDefinition spread)
    {
        var pool = TarotDeck.All.Select(c => c.Id).ToList();
        var result = new List<DrawnCard>();

        foreach (var position in spread.Positions)
        {
            var index = _random.Next(pool.Count);
            var cardId = pool[index];
            pool.RemoveAt(index);

            var isReversed = _random.Next(2) == 1;
            result.Add(new DrawnCard(position.Index, position.Name, cardId, isReversed));
        }

        return result;
    }
}

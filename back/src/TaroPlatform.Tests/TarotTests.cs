using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Tarot;
using TaroPlatform.Domain.Tarot;
using Xunit;

namespace TaroPlatform.Tests;

public class TarotTests
{
    private const string Background = "Вже пів року спілкуюся з людиною, але не розумію, чи є в цьому майбутнє.";

    [Fact]
    public void Deck_HasSeventyEightUniqueCards()
    {
        var ids = TarotDeck.All.Select(c => c.Id).ToList();

        Assert.Equal(78, ids.Count);
        Assert.Equal(78, ids.Distinct().Count());
    }

    [Fact]
    public void EveryTopic_HasStoryAndPositions()
    {
        foreach (var topic in Enum.GetValues<ReadingTopic>())
        {
            var spread = TopicCatalog.Get(topic);

            Assert.NotEmpty(spread.Story);
            Assert.NotEmpty(spread.Positions);
        }
    }

    [Fact]
    public void Draw_ReturnsDistinctCards_OnePerPosition()
    {
        var draw = new CardDrawService(new SeededRandom());
        var spread = TopicCatalog.Get(ReadingTopic.Decision);

        var cards = draw.Draw(spread);

        Assert.Equal(spread.Positions.Count, cards.Count);
        Assert.Equal(cards.Count, cards.Select(c => c.CardId).Distinct().Count());
    }

    [Fact]
    public async Task StartReading_SavesBackgroundQuestionAndAnswerWithProvider()
    {
        var service = CreateService(new FakeTarotRepository());

        var result = await service.StartAsync("user-1",
            new StartReadingRequest(ReadingTopic.Love, Background, "Чи буде у мене стабільний стосунок?"));

        Assert.True(result.IsSuccess);
        Assert.Equal(Background, result.Value.Background);
        Assert.Equal(3, result.Value.Cards.Count);
        Assert.Equal(2, result.Value.Messages.Count);
        Assert.Equal("fake", result.Value.Messages[1].Provider);
    }

    [Fact]
    public async Task StartReading_WithoutBackground_Fails()
    {
        var service = CreateService(new FakeTarotRepository());

        var result = await service.StartAsync("user-1",
            new StartReadingRequest(ReadingTopic.Love, "коротко", "Чи буде у мене стосунок?"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Ask_AddsFollowUp_ToSameReading_AndOtherUserCannotSeeIt()
    {
        var service = CreateService(new FakeTarotRepository());
        var started = await service.StartAsync("user-1",
            new StartReadingRequest(ReadingTopic.Love, Background, "Чи буде у мене стосунок?"));
        var readingId = started.Value.Id;

        var asked = await service.AskAsync("user-1", readingId, new AskRequest("Коли це станеться?"));
        var stranger = await service.AskAsync("user-2", readingId, new AskRequest("Це я?"));

        Assert.True(asked.IsSuccess);
        Assert.Equal(4, asked.Value.Messages.Count);
        Assert.Equal(3, asked.Value.Cards.Count);
        Assert.True(stranger.IsFailure);
    }

    [Fact]
    public void PromptBuilder_FirstAnswer_ContainsBackgroundAndCards()
    {
        var context = new InterpretationContext(
            "Кохання",
            Background,
            "Чи буде стосунок?",
            new List<InterpretedCard> { new("Минуле", "Туз Жезлів", false, "початок") },
            FollowUp: null,
            History: Array.Empty<ChatTurn>());

        var messages = TarotPromptBuilder.Build(context);

        Assert.Equal("system", messages[0].Role);
        Assert.Contains(Background, messages[1].Text);
        Assert.Contains("Туз Жезлів", messages[1].Text);
        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public void PromptBuilder_FollowUp_IsLastAndKeepsHistory()
    {
        var history = new List<ChatTurn>
        {
            new("user", "Чи буде стосунок?"),
            new("assistant", "Відповідь першого кола.")
        };
        var context = new InterpretationContext(
            "Кохання",
            Background,
            "Чи буде стосунок?",
            new List<InterpretedCard> { new("Минуле", "Туз Жезлів", false, "початок") },
            FollowUp: "Коли це станеться?",
            History: history);

        var messages = TarotPromptBuilder.Build(context);

        Assert.Equal("user", messages[^1].Role);
        Assert.Equal("Коли це станеться?", messages[^1].Text);
        Assert.Contains(messages, m => m.Text == "Відповідь першого кола.");
    }

    private static TarotService CreateService(FakeTarotRepository repo) =>
        new(repo, new CardDrawService(new SeededRandom()), new FakeInterpreter());
}

internal class SeededRandom : IRandomSource
{
    private readonly Random _random = new(42);

    public int Next(int maxExclusive) => _random.Next(maxExclusive);
}

internal class FakeInterpreter : ITarotInterpreter
{
    public Task<InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(new InterpretationResult("тестова відповідь", "fake"));
}

internal class FakeTarotRepository : ITarotReadingRepository
{
    private readonly List<TarotReading> _items = new();

    public Task<TarotReading?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(r => r.Id == id));

    public Task<TarotReading?> GetOwnedAsync(string id, string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(r => r.Id == id && r.UserId == userId));

    public Task<IReadOnlyList<TarotReading>> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TarotReading>>(_items.Where(r => r.UserId == userId).ToList());

    public Task<IReadOnlyList<TarotReading>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TarotReading>>(_items.ToList());

    public Task AddAsync(TarotReading entity, CancellationToken cancellationToken = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TarotReading entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _items.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }
}

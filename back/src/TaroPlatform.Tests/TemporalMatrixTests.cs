using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Matrix;
using TaroPlatform.Application.Tarot;
using TaroPlatform.Domain.Matrix;
using Xunit;

namespace TaroPlatform.Tests;

public class TemporalMatrixTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TimeQuestion_UsesTimelinePeriods()
    {
        var interpreter = new CapturingInterpreter();
        var service = new SavedMatrixService(new SavedStore(), interpreter, new Clock(Now));
        var created = await service.SaveAsync("user-1", new SaveMatrixRequest(new DateOnly(1968, 8, 17), null, null));

        await service.AskAsync("user-1", created.Value.Id, new MatrixQuestionRequest("Коли мені чекати змін?", null));

        var cards = interpreter.Last!.Cards;
        Assert.Equal(8, cards.Count);
        Assert.All(cards, c => Assert.StartsWith("Вік", c.PositionName));
        Assert.Contains("поточний період", cards[0].PositionName);
    }

    [Fact]
    public async Task CharacterQuestion_UsesBasePoints()
    {
        var interpreter = new CapturingInterpreter();
        var service = new SavedMatrixService(new SavedStore(), interpreter, new Clock(Now));
        var created = await service.SaveAsync("user-1", new SaveMatrixRequest(new DateOnly(1968, 8, 17), null, null));

        await service.AskAsync("user-1", created.Value.Id, new MatrixQuestionRequest("Який у мене характер?", null));

        Assert.Contains(interpreter.Last!.Cards, c => c.PositionName.StartsWith("Візитна картка"));
        Assert.DoesNotContain(interpreter.Last.Cards, c => c.PositionName.StartsWith("Вік"));
    }

    [Fact]
    public void TemporalDetection_RecognizesTimeWords()
    {
        Assert.True(SavedMatrixService.IsTemporalQuestion("Коли буде фінансовий успіх?"));
        Assert.True(SavedMatrixService.IsTemporalQuestion("У якому віці чекати змін?"));
        Assert.False(SavedMatrixService.IsTemporalQuestion("Який мій талант?"));
    }

    private sealed class Clock : IDateTimeProvider
    {
        public Clock(DateTime now) => UtcNow = now;
        public DateTime UtcNow { get; }
    }

    private sealed class CapturingInterpreter : ITarotInterpreter
    {
        public InterpretationContext? Last { get; private set; }

        public Task<InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default)
        {
            Last = context;
            return Task.FromResult(new InterpretationResult("тест", "fake"));
        }
    }

    private sealed class SavedStore : ISavedMatrixRepository
    {
        private readonly List<SavedMatrix> _items = new();

        public Task<SavedMatrix?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(m => m.Id == id));

        public Task<IReadOnlyList<SavedMatrix>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavedMatrix>>(_items.ToList());

        public Task AddAsync(SavedMatrix entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SavedMatrix entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(m => m.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SavedMatrix>> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SavedMatrix>>(_items.Where(m => m.UserId == userId).ToList());

        public Task<SavedMatrix?> GetOwnedAsync(string id, string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(m => m.Id == id && m.UserId == userId));
    }
}

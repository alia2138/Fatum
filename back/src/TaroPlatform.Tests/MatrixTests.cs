using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Matrix;
using TaroPlatform.Application.Tarot;
using TaroPlatform.Domain.Matrix;
using Xunit;

namespace TaroPlatform.Tests;

public class MatrixTests
{
    private static readonly DateOnly Sample = new(1993, 8, 25);
    private static readonly DateOnly Kuzma = new(1968, 8, 17);

    [Fact]
    public void Reduce_FollowsRuleOf22()
    {
        Assert.Equal(22, DestinyMatrixCalculator.Reduce(22));
        Assert.Equal(5, DestinyMatrixCalculator.Reduce(23));
        Assert.Equal(10, DestinyMatrixCalculator.Reduce(37));
        Assert.Equal(11, DestinyMatrixCalculator.Reduce(47));
    }

    [Fact]
    public void DocumentMethod_MatchesDocumentExample()
    {
        var m = DestinyMatrixCalculator.Calculate(Sample, ChannelMethod.Document);

        Assert.Equal(7, m["A"]);
        Assert.Equal(8, m["B"]);
        Assert.Equal(22, m["V"]);
        Assert.Equal(10, m["G"]);
        Assert.Equal(11, m["D"]);
        Assert.Equal(21, m["E"]);
        Assert.Equal(7, m["Rel"]);
        Assert.Equal(5, m["Money"]);
        Assert.Equal(11, m["Personal"]);
        Assert.Equal(4, m["Social"]);
        Assert.Equal(15, m["Spiritual"]);
    }

    [Fact]
    public void VideoMethod_MatchesKuzmaExample()
    {
        // Значення з відеоуроку для 17.08.1968
        var m = DestinyMatrixCalculator.Calculate(Kuzma, ChannelMethod.Video);

        Assert.Equal(17, m["A"]);        // День
        Assert.Equal(8, m["B"]);         // Місяць
        Assert.Equal(6, m["V"]);         // Рік 1968 → 24 → 6
        Assert.Equal(4, m["G"]);         // Нижнє число
        Assert.Equal(8, m["D"]);         // Центр
        Assert.Equal(7, m["Zh"]);        // Вища суть
        Assert.Equal(12, m["Heaven"]);   // Небо
        Assert.Equal(5, m["Earth"]);     // Земля
        Assert.Equal(17, m["Personal"]); // Особисте призначення
        Assert.Equal(17, m["Male"]);     // Чоловіча лінія
        Assert.Equal(8, m["Female"]);    // Жіноча лінія
        Assert.Equal(7, m["Social"]);    // Соціальне призначення
        Assert.Equal(6, m["Spiritual"]); // Духовне призначення
        Assert.Equal(20, m["Rel"]);      // Точка кохання
        Assert.Equal(14, m["Money"]);    // Грошовий канал: перша точка
        Assert.Equal(8, m["MoneyMid"]);  // Грошовий канал: друга точка
        Assert.Equal(22, m["MoneyEnd"]); // Грошовий канал: третя точка
    }

    [Fact]
    public void PointCount_DependsOnMethod()
    {
        Assert.Equal(19, DestinyMatrixCalculator.Calculate(Sample, ChannelMethod.Document).Points.Count);
        Assert.Equal(21, DestinyMatrixCalculator.Calculate(Sample, ChannelMethod.Video).Points.Count);
    }

    [Fact]
    public void AgeValues_MatchDiagramAndRules()
    {
        var m = DestinyMatrixCalculator.Calculate(Sample, ChannelMethod.Document);

        Assert.Equal(22, DestinyMatrixCalculator.ValueAt(m, 5));
        Assert.Equal(5, DestinyMatrixCalculator.ValueAt(m, 15));
        Assert.Equal(11, DestinyMatrixCalculator.ValueAt(m, 25));
        Assert.Equal(7, DestinyMatrixCalculator.ValueAt(m, 35));
        Assert.Equal(9, DestinyMatrixCalculator.ValueAt(m, 45));
        Assert.Equal(15, DestinyMatrixCalculator.ValueAt(m, 55));
        Assert.Equal(9, DestinyMatrixCalculator.ValueAt(m, 65));
        Assert.Equal(6, DestinyMatrixCalculator.ValueAt(m, 75));
        Assert.Equal(19, DestinyMatrixCalculator.ValueAt(m, 22.5));
    }

    [Fact]
    public void Timeline_Has32PeriodsOf2_5Years()
    {
        var timeline = DestinyMatrixCalculator.Timeline(DestinyMatrixCalculator.Calculate(Sample));

        Assert.Equal(32, timeline.Count);
        Assert.Equal(0, timeline[0].From);
        Assert.Equal(2.5, timeline[0].To);
    }

    [Fact]
    public async Task Calculate_RejectsFutureBirthDate()
    {
        var service = CreateService();

        var result = await service.CalculateAsync(new DateOnly(2027, 1, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Calculate_ReturnsPointsTimelineAndInterpretation()
    {
        var service = CreateService();

        var result = await service.CalculateAsync(Kuzma);

        Assert.True(result.IsSuccess);
        Assert.Equal("Video", result.Value.Method);
        Assert.Equal(21, result.Value.Points.Count);
        Assert.Equal(32, result.Value.Timeline.Count);
        Assert.Equal("тест", result.Value.Interpretation);
    }

    [Fact]
    public async Task Compatibility_IsSymmetric()
    {
        var service = CreateService();
        var other = new DateOnly(1990, 3, 14);

        var ab = await service.CompatibilityAsync(Sample, other);
        var ba = await service.CompatibilityAsync(other, Sample);

        Assert.Equal(
            ab.Value.Points.Select(p => p.Value),
            ba.Value.Points.Select(p => p.Value));
    }

    private static MatrixService CreateService() =>
        new(new FakeInterpreter(), new FakeUsers(), new FixedClock(new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc)));

    private sealed class FixedClock : IDateTimeProvider
    {
        public FixedClock(DateTime now) => UtcNow = now;
        public DateTime UtcNow { get; }
    }

    private sealed class FakeInterpreter : ITarotInterpreter
    {
        public Task<InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new InterpretationResult("тест", "fake"));
    }

    /// <summary>Порожній репозиторій: розрахунок за датою не потребує користувачів.</summary>
    private sealed class FakeUsers : IUserRepository
    {
        public Task<TaroPlatform.Domain.Users.User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<TaroPlatform.Domain.Users.User?>(null);
        public Task<TaroPlatform.Domain.Users.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<TaroPlatform.Domain.Users.User?>(null);
        public Task<TaroPlatform.Domain.Users.User?> GetByExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TaroPlatform.Domain.Users.User?>(null);
        public Task<(IReadOnlyList<TaroPlatform.Domain.Users.Consultant> Items, long Total)> SearchConsultantsAsync(TaroPlatform.Application.Catalog.ConsultantSearch search, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<TaroPlatform.Domain.Users.Consultant>, long)>((new List<TaroPlatform.Domain.Users.Consultant>(), 0L));
        public Task<IReadOnlyList<TaroPlatform.Domain.Users.User>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TaroPlatform.Domain.Users.User>>(new List<TaroPlatform.Domain.Users.User>());
        public Task AddAsync(TaroPlatform.Domain.Users.User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(TaroPlatform.Domain.Users.User entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

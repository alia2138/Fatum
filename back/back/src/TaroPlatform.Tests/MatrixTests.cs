using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Matrix;
using TaroPlatform.Application.Tarot;
using TaroPlatform.Domain.Matrix;
using Xunit;

namespace TaroPlatform.Tests;

public class MatrixTests
{
    private static readonly DateOnly Sample = new(1993, 8, 25);

    [Fact]
    public void Reduce_FollowsRuleOf22()
    {
        Assert.Equal(22, DestinyMatrixCalculator.Reduce(22));
        Assert.Equal(5, DestinyMatrixCalculator.Reduce(23));
        Assert.Equal(10, DestinyMatrixCalculator.Reduce(37));
        Assert.Equal(11, DestinyMatrixCalculator.Reduce(47));
    }

    [Fact]
    public void Calculate_MatchesManualExample_ForBirthDate()
    {
        var m = DestinyMatrixCalculator.Calculate(Sample);

        Assert.Equal(7, m["A"]);       // День 25 → 7
        Assert.Equal(8, m["B"]);       // Серпень
        Assert.Equal(22, m["V"]);      // 1+9+9+3 = 22
        Assert.Equal(10, m["G"]);      // 7+8+22 = 37 → 10
        Assert.Equal(11, m["D"]);      // 7+8+22+10 = 47 → 11
        Assert.Equal(21, m["E"]);      // 11+10
        Assert.Equal(7, m["Rel"]);     // 21+22 = 43 → 7
        Assert.Equal(5, m["Money"]);   // 21+11 = 32 → 5
        Assert.Equal(11, m["Personal"]);
        Assert.Equal(4, m["Social"]);
        Assert.Equal(15, m["Spiritual"]);
    }

    [Fact]
    public void AgeValues_MatchDiagramAndRules()
    {
        var m = DestinyMatrixCalculator.Calculate(Sample);

        Assert.Equal(22, DestinyMatrixCalculator.ValueAt(m, 5));   // 7 + 15
        Assert.Equal(5, DestinyMatrixCalculator.ValueAt(m, 15));   // 15 + 8 = 23 → 5
        Assert.Equal(11, DestinyMatrixCalculator.ValueAt(m, 25));  // 8 + 3
        Assert.Equal(7, DestinyMatrixCalculator.ValueAt(m, 35));   // 3 + 22 = 25 → 7
        Assert.Equal(9, DestinyMatrixCalculator.ValueAt(m, 45));   // 22 + 5 = 27 → 9
        Assert.Equal(15, DestinyMatrixCalculator.ValueAt(m, 55));  // 5 + 10
        Assert.Equal(9, DestinyMatrixCalculator.ValueAt(m, 65));   // 10 + 17 = 27 → 9
        Assert.Equal(6, DestinyMatrixCalculator.ValueAt(m, 75));   // 17 + 7 = 24 → 6
        Assert.Equal(19, DestinyMatrixCalculator.ValueAt(m, 22.5)); // 8 + 11
    }

    [Fact]
    public void Timeline_Has32PeriodsOf2_5Years()
    {
        var m = DestinyMatrixCalculator.Calculate(Sample);

        var timeline = DestinyMatrixCalculator.Timeline(m);

        Assert.Equal(32, timeline.Count);
        Assert.Equal(0, timeline[0].From);
        Assert.Equal(2.5, timeline[0].To);
    }

    [Fact]
    public async Task Calculate_RejectsFutureBirthDate()
    {
        var service = new MatrixService(new FakeInterpreter(), new FakeUsers(), new FixedClock(new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc)));

        var result = await service.CalculateAsync(new DateOnly(2027, 1, 1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Calculate_ReturnsPointsTimelineAndInterpretation()
    {
        var service = new MatrixService(new FakeInterpreter(), new FakeUsers(), new FixedClock(new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc)));

        var result = await service.CalculateAsync(Sample);

        Assert.True(result.IsSuccess);
        Assert.Equal(19, result.Value.Points.Count);
        Assert.Equal(32, result.Value.Timeline.Count);
        Assert.Equal("тест", result.Value.Interpretation);
    }

    [Fact]
    public async Task Compatibility_IsSymmetric()
    {
        var service = new MatrixService(new FakeInterpreter(), new FakeUsers(), new FixedClock(new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc)));
        var other = new DateOnly(1990, 3, 14);

        var ab = await service.CompatibilityAsync(Sample, other);
        var ba = await service.CompatibilityAsync(other, Sample);

        Assert.Equal(
            ab.Value.Points.Select(p => p.Value),
            ba.Value.Points.Select(p => p.Value));
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public FixedClock(DateTime now) => UtcNow = now;
        public DateTime UtcNow { get; }
    }

    private sealed class FakeInterpreter : ITarotInterpreter
    {
        public Task<TaroPlatform.Application.Tarot.InterpretationResult> InterpretAsync(InterpretationContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TaroPlatform.Application.Tarot.InterpretationResult("тест", "fake"));
    }

    /// <summary>Порожній репозиторій: сервіс матриці користувачів не потребує для розрахунку за датою.</summary>
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

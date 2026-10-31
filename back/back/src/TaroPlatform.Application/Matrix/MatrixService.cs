using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Application.Tarot;
using TaroPlatform.Domain.Matrix;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Matrix;

public interface IMatrixService
{
    Task<Result<MatrixResultDto>> CalculateAsync(DateOnly birthDate, CancellationToken cancellationToken = default);
    Task<Result<MatrixResultDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<CompatibilityResultDto>> CompatibilityAsync(DateOnly first, DateOnly second, CancellationToken cancellationToken = default);
}

public class MatrixService : IMatrixService
{
    private static readonly DateOnly EarliestBirthDate = new(1900, 1, 1);

    /// <summary>Точки, які беруть участь у сумісності двох людей.</summary>
    private static readonly string[] CompatibilityKeys = { "A", "B", "V", "G", "D", "Rel", "Money" };

    private readonly ITarotInterpreter _interpreter;
    private readonly IUserRepository _users;
    private readonly IDateTimeProvider _clock;

    public MatrixService(ITarotInterpreter interpreter, IUserRepository users, IDateTimeProvider clock)
    {
        _interpreter = interpreter;
        _users = users;
        _clock = clock;
    }

    public async Task<Result<MatrixResultDto>> CalculateAsync(DateOnly birthDate, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        if (birthDate < EarliestBirthDate || birthDate > today)
            return Result.Failure<MatrixResultDto>("Некоректна дата народження.");

        var matrix = DestinyMatrixCalculator.Calculate(birthDate);

        var cards = matrix.Points.Count > 0
            ? DescribePoints(matrix, PersonalKeys)
            : new List<InterpretedCard>();

        var interpretation = await _interpreter.InterpretAsync(
            new InterpretationContext(
                "Матриця долі",
                $"Дата народження: {birthDate:dd.MM.yyyy}",
                "Персональний опис матриці долі: характер, призначення і поточний період життя",
                cards,
                FollowUp: null,
                History: Array.Empty<ChatTurn>()),
            cancellationToken);

        var result = new MatrixResultDto(
            birthDate,
            DescribeAll(matrix),
            DestinyMatrixCalculator.Timeline(matrix).Select(p => ToTimelineDto(p)).ToList(),
            BuildCurrent(matrix, birthDate, today),
            interpretation.Text,
            interpretation.Provider);

        return Result.Success(result);
    }

    public async Task<Result<MatrixResultDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<MatrixResultDto>("Користувача не знайдено.");
        if (user is not Client client)
            return Result.Failure<MatrixResultDto>("Матриця долі будується за датою народження клієнта.");

        return await CalculateAsync(DateOnly.FromDateTime(client.BirthDate), cancellationToken);
    }

    public async Task<Result<CompatibilityResultDto>> CompatibilityAsync(DateOnly first, DateOnly second, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        if (first < EarliestBirthDate || first > today || second < EarliestBirthDate || second > today)
            return Result.Failure<CompatibilityResultDto>("Некоректна дата народження.");

        var m1 = DestinyMatrixCalculator.Calculate(first);
        var m2 = DestinyMatrixCalculator.Calculate(second);

        var points = new List<CompatibilityPointDto>();
        var cards = new List<InterpretedCard>();

        foreach (var key in CompatibilityKeys)
        {
            // Сумісність у точці: сума значень двох людей у цій точці
            var value = DestinyMatrixCalculator.Reduce(m1[key] + m2[key]);
            var arcana = DestinyMatrixCalculator.ArcanaFor(value);
            var title = DestinyMatrixCalculator.PointDefinitions.First(p => p.Key == key).Title;

            points.Add(new CompatibilityPointDto(key, title, value, arcana.Name, arcana.UprightMeaning));
            cards.Add(new InterpretedCard(title, arcana.Name, false, arcana.UprightMeaning));
        }

        var interpretation = await _interpreter.InterpretAsync(
            new InterpretationContext(
                "Сумісність",
                $"Дати народження: {first:dd.MM.yyyy} та {second:dd.MM.yyyy}",
                "Опиши сумісність цих двох людей: сильні сторони, можливі виклики і поради",
                cards,
                FollowUp: null,
                History: Array.Empty<ChatTurn>()),
            cancellationToken);

        return Result.Success(new CompatibilityResultDto(
            first,
            second,
            points,
            interpretation.Text,
            interpretation.Provider));
    }

    // ---------- Допоміжні ----------

    private static readonly string[] PersonalKeys = { "A", "B", "V", "G", "D", "Zh", "Z", "I", "K", "Rel", "Money" };

    private static IReadOnlyList<MatrixPointDto> DescribeAll(DestinyMatrix matrix) =>
        DestinyMatrixCalculator.PointDefinitions
            .Select(def =>
            {
                var value = matrix[def.Key];
                var arcana = DestinyMatrixCalculator.ArcanaFor(value);
                return new MatrixPointDto(def.Key, def.Title, def.Group, value, arcana.Name, arcana.UprightMeaning);
            })
            .ToList();

    private static List<InterpretedCard> DescribePoints(DestinyMatrix matrix, IEnumerable<string> keys)
    {
        var cards = new List<InterpretedCard>();
        foreach (var key in keys)
        {
            var title = DestinyMatrixCalculator.PointDefinitions.First(p => p.Key == key).Title;
            var arcana = DestinyMatrixCalculator.ArcanaFor(matrix[key]);
            cards.Add(new InterpretedCard(title, arcana.Name, false, arcana.UprightMeaning));
        }
        return cards;
    }

    private static TimelinePeriodDto ToTimelineDto(AgePeriod period)
    {
        var arcana = DestinyMatrixCalculator.ArcanaFor(period.Value);
        return new TimelinePeriodDto(
            period.From,
            period.To,
            period.Value,
            arcana.Name,
            DestinyMatrixCalculator.LineFor(period.From));
    }

    private static CurrentPeriodDto BuildCurrent(DestinyMatrix matrix, DateOnly birthDate, DateOnly today)
    {
        var ageYears = (today.DayNumber - birthDate.DayNumber) / 365.25;
        var period = DestinyMatrixCalculator.PeriodAt(matrix, ageYears);
        var arcana = DestinyMatrixCalculator.ArcanaFor(period.Value);

        return new CurrentPeriodDto(
            Math.Round(ageYears, 1),
            period.From,
            period.To,
            period.Value,
            arcana.Name,
            arcana.UprightMeaning,
            DestinyMatrixCalculator.LineFor(period.From));
    }
}

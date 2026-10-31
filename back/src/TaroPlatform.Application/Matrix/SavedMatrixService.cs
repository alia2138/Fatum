using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Application.Tarot;
using TaroPlatform.Domain.Matrix;

namespace TaroPlatform.Application.Matrix;

public interface ISavedMatrixService
{
    Task<Result<SavedMatrixSummaryDto>> SaveAsync(string userId, SaveMatrixRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedMatrixSummaryDto>> ListAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<SavedMatrixDetailDto>> GetAsync(string userId, string id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ZoneAnswerDto>>> GetZonesAsync(string userId, string id, CancellationToken cancellationToken = default);
    Task<Result<SavedMatrixDetailDto>> AskAsync(string userId, string id, MatrixQuestionRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string userId, string id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Збережені матриці і розмова з AI про них. AI бачить усі точки з арканами і значеннями,
/// а якщо вказана зона, то відповідає з фокусом на її точках.
/// </summary>
public class SavedMatrixService : ISavedMatrixService
{
    private static readonly DateOnly EarliestBirthDate = new(1900, 1, 1);
    private const int MinQuestionLength = 3;
    private const int MaxQuestionLength = 1000;
    private const int MaxLabelLength = 60;

    private readonly ISavedMatrixRepository _saved;
    private readonly ITarotInterpreter _interpreter;
    private readonly IDateTimeProvider _clock;

    public SavedMatrixService(ISavedMatrixRepository saved, ITarotInterpreter interpreter, IDateTimeProvider clock)
    {
        _saved = saved;
        _interpreter = interpreter;
        _clock = clock;
    }

    public async Task<Result<SavedMatrixSummaryDto>> SaveAsync(string userId, SaveMatrixRequest request, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        if (request.BirthDate < EarliestBirthDate || request.BirthDate > today)
            return Result.Failure<SavedMatrixSummaryDto>("Некоректна дата народження.");

        var label = string.IsNullOrWhiteSpace(request.Label)
            ? null
            : request.Label.Trim()[..Math.Min(request.Label.Trim().Length, MaxLabelLength)];

        var saved = new SavedMatrix(
            userId,
            request.BirthDate.ToDateTime(TimeOnly.MinValue),
            request.Method ?? ChannelMethod.Video,
            label);

        await _saved.AddAsync(saved, cancellationToken);
        return Result.Success(ToSummary(saved));
    }

    public async Task<IReadOnlyList<SavedMatrixSummaryDto>> ListAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await _saved.GetByUserAsync(userId, cancellationToken);
        return items.Select(ToSummary).ToList();
    }

    public async Task<Result<SavedMatrixDetailDto>> GetAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        var saved = await _saved.GetOwnedAsync(id, userId, cancellationToken);
        if (saved is null)
            return Result.Failure<SavedMatrixDetailDto>("Матрицю не знайдено.");

        return Result.Success(ToDetail(saved));
    }

    public async Task<Result<IReadOnlyList<ZoneAnswerDto>>> GetZonesAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        var saved = await _saved.GetOwnedAsync(id, userId, cancellationToken);
        if (saved is null)
            return Result.Failure<IReadOnlyList<ZoneAnswerDto>>("Матрицю не знайдено.");

        var matrix = Calculate(saved);
        IReadOnlyList<ZoneAnswerDto> zones = MatrixZones.All
            .Select(z => ZoneAnswer(z, matrix))
            .ToList();

        return Result.Success(zones);
    }

    public async Task<Result<SavedMatrixDetailDto>> AskAsync(string userId, string id, MatrixQuestionRequest request, CancellationToken cancellationToken = default)
    {
        var saved = await _saved.GetOwnedAsync(id, userId, cancellationToken);
        if (saved is null)
            return Result.Failure<SavedMatrixDetailDto>("Матрицю не знайдено.");

        var text = (request.Question ?? string.Empty).Trim();
        if (text.Length < MinQuestionLength || text.Length > MaxQuestionLength)
            return Result.Failure<SavedMatrixDetailDto>($"Питання має містити від {MinQuestionLength} до {MaxQuestionLength} символів.");

        MatrixZones.Zone? zone = null;
        if (!string.IsNullOrWhiteSpace(request.ZoneKey))
        {
            zone = MatrixZones.Find(request.ZoneKey);
            if (zone is null)
                return Result.Failure<SavedMatrixDetailDto>("Невідома зона матриці.");
        }

        var matrix = Calculate(saved);
        var birth = DateOnly.FromDateTime(saved.BirthDate);
        var question = zone is null ? text : $"Зона: {zone.Title}. Питання: {text}";

        var history = saved.Messages
            .Select(m => new ChatTurn(m.Role == MatrixMessageRole.User ? "user" : "assistant", m.Text))
            .ToList();

        var result = await _interpreter.InterpretAsync(
            new InterpretationContext(
                "Матриця долі",
                $"Дата народження: {birth:dd.MM.yyyy}; спосіб розрахунку: {saved.Method}",
                question,
                BuildCards(matrix, zone),
                FollowUp: null,
                History: history),
            cancellationToken);

        saved.AddMessage(MatrixMessageRole.User, text, zone?.Key);
        saved.AddMessage(MatrixMessageRole.Assistant, result.Text, zone?.Key, result.Provider);
        await _saved.UpdateAsync(saved, cancellationToken);

        return Result.Success(ToDetail(saved));
    }

    public async Task<Result> DeleteAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        var saved = await _saved.GetOwnedAsync(id, userId, cancellationToken);
        if (saved is null)
            return Result.Failure("Матрицю не знайдено.");

        await _saved.DeleteAsync(saved.Id, cancellationToken);
        return Result.Success();
    }

    // ---------- Допоміжні ----------

    private static DestinyMatrix Calculate(SavedMatrix saved) =>
        DestinyMatrixCalculator.Calculate(DateOnly.FromDateTime(saved.BirthDate), saved.Method);

    private static SavedMatrixSummaryDto ToSummary(SavedMatrix saved) =>
        new(
            saved.Id,
            DateOnly.FromDateTime(saved.BirthDate),
            saved.Method.ToString(),
            saved.Label,
            saved.CreatedAt,
            saved.Messages.Count(m => m.Role == MatrixMessageRole.User));

    private SavedMatrixDetailDto ToDetail(SavedMatrix saved)
    {
        var matrix = Calculate(saved);
        var birth = DateOnly.FromDateTime(saved.BirthDate);
        var today = DateOnly.FromDateTime(_clock.UtcNow);

        var messages = saved.Messages
            .Select(m => new MatrixMessageDto(m.Role.ToString(), m.Text, m.Zone, m.Provider, m.CreatedAt))
            .ToList();

        return new SavedMatrixDetailDto(
            saved.Id,
            birth,
            saved.Method.ToString(),
            saved.Label,
            saved.CreatedAt,
            MatrixDescriber.Points(matrix),
            MatrixDescriber.Timeline(matrix),
            MatrixDescriber.Current(matrix, birth, today),
            messages);
    }

    /// <summary>Усі точки для AI. Точки обраної зони позначаються як фокус.</summary>
    private static List<InterpretedCard> BuildCards(DestinyMatrix matrix, MatrixZones.Zone? zone)
    {
        var cards = new List<InterpretedCard>();

        foreach (var definition in DestinyMatrixCalculator.PointDefinitions.Where(d => matrix.Points.ContainsKey(d.Key)))
        {
            var arcana = DestinyMatrixCalculator.ArcanaFor(matrix[definition.Key]);
            var inFocus = zone is not null && zone.PointKeys.Contains(definition.Key);
            var position = inFocus ? $"{definition.Title} (фокус: {zone!.Title})" : definition.Title;

            cards.Add(new InterpretedCard(position, arcana.Name, false, arcana.UprightMeaning));
        }

        return cards;
    }

    private static ZoneAnswerDto ZoneAnswer(MatrixZones.Zone zone, DestinyMatrix matrix)
    {
        var points = zone.PointKeys
            .Where(k => matrix.Points.ContainsKey(k))
            .Select(k => MatrixDescriber.Point(k, matrix))
            .ToList();

        var lines = points.Select(p => $"• {p.Title}: {p.Value} — {p.Arcana}. {p.ArcanaMeaning}");
        var text = $"{zone.Title}. {zone.Description}\n" + string.Join("\n", lines);

        return new ZoneAnswerDto(zone.Key, zone.Title, zone.Description, points, text);
    }
}

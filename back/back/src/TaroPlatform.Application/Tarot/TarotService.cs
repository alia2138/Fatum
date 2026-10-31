using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Application.Tarot;

public interface ITarotService
{
    IReadOnlyList<TopicDto> GetTopics();
    Task<Result<ReadingDetailDto>> StartAsync(string userId, StartReadingRequest request, CancellationToken cancellationToken = default);
    Task<Result<ReadingDetailDto>> AskAsync(string userId, string readingId, AskRequest request, CancellationToken cancellationToken = default);
    Task<Result<ReadingDetailDto>> GetAsync(string userId, string readingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReadingSummaryDto>> GetHistoryAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string userId, string readingId, CancellationToken cancellationToken = default);
}

public class TarotService : ITarotService
{
    private const int MinBackgroundLength = 10;
    private const int MaxBackgroundLength = 1500;
    private const int MinQuestionLength = 3;
    private const int MaxQuestionLength = 500;

    private readonly ITarotReadingRepository _readings;
    private readonly CardDrawService _draw;
    private readonly ITarotInterpreter _interpreter;

    public TarotService(ITarotReadingRepository readings, CardDrawService draw, ITarotInterpreter interpreter)
    {
        _readings = readings;
        _draw = draw;
        _interpreter = interpreter;
    }

    public IReadOnlyList<TopicDto> GetTopics() =>
        TopicCatalog.All
            .Select(s => new TopicDto(
                s.Topic.ToString(),
                s.Title,
                s.Story,
                s.Positions.Select(p => p.Name).ToList()))
            .ToList();

    public async Task<Result<ReadingDetailDto>> StartAsync(string userId, StartReadingRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Topic))
            return Result.Failure<ReadingDetailDto>("Невідома тема гадання.");

        var background = (request.Background ?? string.Empty).Trim();
        if (background.Length < MinBackgroundLength)
            return Result.Failure<ReadingDetailDto>(
                $"Розкажіть передісторію: щонайменше {MinBackgroundLength} символів, щоб трактування було зрозумілим.");
        if (background.Length > MaxBackgroundLength)
            return Result.Failure<ReadingDetailDto>(
                $"Передісторія не може бути довшою за {MaxBackgroundLength} символів.");

        var question = (request.Question ?? string.Empty).Trim();
        var error = ValidateQuestion(question);
        if (error is not null)
            return Result.Failure<ReadingDetailDto>(error);

        var spread = TopicCatalog.Get(request.Topic);
        var drawn = _draw.Draw(spread);

        var result = await _interpreter.InterpretAsync(
            new InterpretationContext(
                spread.Title,
                background,
                question,
                ToInterpreted(drawn),
                FollowUp: null,
                History: Array.Empty<ChatTurn>()),
            cancellationToken);

        var reading = new TarotReading(userId, request.Topic, background, question, drawn.ToList());
        reading.AddMessage(MessageRole.User, question);
        reading.AddMessage(MessageRole.Assistant, result.Text, result.Provider);

        await _readings.AddAsync(reading, cancellationToken);

        return Result.Success(ToDetail(reading));
    }

    /// <summary>
    /// Додаткове питання: витягуємо 1–3 нові карти (без повторів). Модель отримує передісторію,
    /// головне питання, усю попередню розмову і нові карти.
    /// </summary>
    public async Task<Result<ReadingDetailDto>> AskAsync(string userId, string readingId, AskRequest request, CancellationToken cancellationToken = default)
    {
        var reading = await _readings.GetOwnedAsync(readingId, userId, cancellationToken);
        if (reading is null)
            return Result.Failure<ReadingDetailDto>("Гадання не знайдено.");

        var text = (request.Question ?? string.Empty).Trim();
        var error = ValidateQuestion(text);
        if (error is not null)
            return Result.Failure<ReadingDetailDto>(error);

        var spread = TopicCatalog.Get(reading.Topic);
        var round = reading.Cards.Max(c => c.Round) + 1;
        var newCards = _draw.DrawFollowUp(reading.Cards.Select(c => c.CardId), round);

        // Уся попередня розмова: головне питання, відповіді, попередні уточнення з відповідями
        var history = reading.Messages
            .Select(m => new ChatTurn(m.Role == MessageRole.User ? "user" : "assistant", m.Text))
            .ToList();

        var result = await _interpreter.InterpretAsync(
            new InterpretationContext(
                spread.Title,
                reading.Background,
                reading.Question,
                ToInterpreted(newCards),
                FollowUp: text,
                History: history),
            cancellationToken);

        reading.Cards.AddRange(newCards);
        reading.AddMessage(MessageRole.User, text);
        reading.AddMessage(MessageRole.Assistant, result.Text, result.Provider);

        await _readings.UpdateAsync(reading, cancellationToken);

        return Result.Success(ToDetail(reading));
    }

    public async Task<Result<ReadingDetailDto>> GetAsync(string userId, string readingId, CancellationToken cancellationToken = default)
    {
        var reading = await _readings.GetOwnedAsync(readingId, userId, cancellationToken);
        if (reading is null)
            return Result.Failure<ReadingDetailDto>("Гадання не знайдено.");

        return Result.Success(ToDetail(reading));
    }

    public async Task<IReadOnlyList<ReadingSummaryDto>> GetHistoryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var readings = await _readings.GetByUserAsync(userId, cancellationToken);

        return readings
            .Select(r => new ReadingSummaryDto(
                r.Id,
                r.Topic.ToString(),
                TopicCatalog.Get(r.Topic).Title,
                r.Question,
                r.CreatedAt,
                r.Cards.Count))
            .ToList();
    }

    public async Task<Result> DeleteAsync(string userId, string readingId, CancellationToken cancellationToken = default)
    {
        var reading = await _readings.GetOwnedAsync(readingId, userId, cancellationToken);
        if (reading is null)
            return Result.Failure("Гадання не знайдено.");

        await _readings.DeleteAsync(reading.Id, cancellationToken);
        return Result.Success();
    }

    private static string? ValidateQuestion(string question)
    {
        if (question.Length < MinQuestionLength)
            return $"Запитання має містити щонайменше {MinQuestionLength} символи.";

        if (question.Length > MaxQuestionLength)
            return $"Запитання не може бути довшим за {MaxQuestionLength} символів.";

        return null;
    }

    private static IReadOnlyList<InterpretedCard> ToInterpreted(IEnumerable<DrawnCard> cards) =>
        cards
            .Select(c =>
            {
                var card = TarotDeck.GetById(c.CardId);
                return new InterpretedCard(c.PositionName, card.Name, c.IsReversed, card.GetMeaning(c.IsReversed));
            })
            .ToList();

    private static ReadingDetailDto ToDetail(TarotReading reading)
    {
        var spread = TopicCatalog.Get(reading.Topic);

        var cards = reading.Cards
            .Select(c =>
            {
                var card = TarotDeck.GetById(c.CardId);
                return new DrawnCardDto(
                    c.Position,
                    c.PositionName,
                    card.Name,
                    card.ArcanaType,
                    c.IsReversed,
                    card.GetMeaning(c.IsReversed),
                    c.Round);
            })
            .ToList();

        var messages = reading.Messages
            .Select(m => new MessageDto(m.Role.ToString(), m.Text, m.Provider, m.CreatedAt))
            .ToList();

        return new ReadingDetailDto(
            reading.Id,
            reading.Topic.ToString(),
            spread.Title,
            reading.Background,
            reading.Question,
            reading.CreatedAt,
            cards,
            messages);
    }
}

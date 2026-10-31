using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Application.Notifications;
using TaroPlatform.Domain.Consultations;
using TaroPlatform.Domain.Reviews;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Reviews;

public interface IReviewService
{
    Task<Result<ReviewDto>> CreateAsync(string clientId, ReviewCreateRequest request, CancellationToken cancellationToken = default);
    Task<Result<ReviewDto>> UpdateAsync(string clientId, string reviewId, ReviewUpdateRequest request, CancellationToken cancellationToken = default);
    Task<Result<ReviewDto>> ReplyAsync(string consultantId, string reviewId, ReviewReplyRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ReviewDto>>> ListForConsultantAsync(string consultantId, int page, int pageSize, CancellationToken cancellationToken = default);
}

public class ReviewService : IReviewService
{
    private const int EditWindowDays = 14;
    private const int MaxTextLength = 1000;
    private const int MaxReplyLength = 500;
    private const int MaxPageSize = 50;

    private readonly IReviewRepository _reviews;
    private readonly IConsultationRepository _consultations;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _clock;

    public ReviewService(
        IReviewRepository reviews,
        IConsultationRepository consultations,
        IUserRepository users,
        INotificationService notifications,
        IDateTimeProvider clock)
    {
        _reviews = reviews;
        _consultations = consultations;
        _users = users;
        _notifications = notifications;
        _clock = clock;
    }

    /// <summary>Клієнт залишає відгук на проведену консультацію.</summary>
    public async Task<Result<ReviewDto>> CreateAsync(string clientId, ReviewCreateRequest request, CancellationToken cancellationToken = default)
    {
        var client = await _users.GetByIdAsync(clientId, cancellationToken);
        if (client is not Client)
            return Fail("Відгуки можуть залишати лише клієнти.");

        if (!IsValidRating(request.Rating))
            return Fail("Оцінка має бути від 1 до 5.");

        var text = NormalizeText(request.Text, MaxTextLength);
        if (text.Error is not null)
            return Fail(text.Error);

        var consultation = await _consultations.GetByIdAsync(request.ConsultationId, cancellationToken);
        if (consultation is null || consultation.ClientId != clientId)
            return Fail("Консультацію не знайдено.");
        if (consultation.Status != ConsultationStatus.Completed)
            return Fail("Відгук можна залишити лише після проведеної консультації.");
        if (await _reviews.GetByConsultationAsync(consultation.Id, cancellationToken) is not null)
            return Fail("Ви вже залишили відгук на цю консультацію.");

        var review = new Review(consultation.Id, clientId, consultation.ConsultantId, request.Rating, text.Value);
        await _reviews.AddAsync(review, cancellationToken);
        await RecalculateRatingAsync(consultation.ConsultantId, cancellationToken);

        await _notifications.NotifyAsync(
            consultation.ConsultantId,
            "Новий відгук",
            $"{client.DisplayName} поставив оцінку {request.Rating} з 5.",
            null,
            cancellationToken);

        return Result.Success(await ToDtoAsync(review, cancellationToken));
    }

    /// <summary>Редагування свого відгуку протягом 14 днів після публікації.</summary>
    public async Task<Result<ReviewDto>> UpdateAsync(string clientId, string reviewId, ReviewUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var review = await _reviews.GetByIdAsync(reviewId, cancellationToken);
        if (review is null || review.ClientId != clientId)
            return Fail("Відгук не знайдено.");

        if (review.CreatedAt < _clock.UtcNow.AddDays(-EditWindowDays))
            return Fail($"Відгук можна редагувати протягом {EditWindowDays} днів після публікації.");

        var rating = request.Rating ?? review.Rating;
        if (!IsValidRating(rating))
            return Fail("Оцінка має бути від 1 до 5.");

        var newText = review.Text;
        if (request.Text is not null)
        {
            var text = NormalizeText(request.Text, MaxTextLength);
            if (text.Error is not null)
                return Fail(text.Error);
            newText = text.Value;
        }

        review.Update(rating, newText, _clock.UtcNow);
        await _reviews.UpdateAsync(review, cancellationToken);
        await RecalculateRatingAsync(review.ConsultantId, cancellationToken);

        return Result.Success(await ToDtoAsync(review, cancellationToken));
    }

    /// <summary>Таролог відповідає на відгук. Одна відповідь на відгук.</summary>
    public async Task<Result<ReviewDto>> ReplyAsync(string consultantId, string reviewId, ReviewReplyRequest request, CancellationToken cancellationToken = default)
    {
        var review = await _reviews.GetByIdAsync(reviewId, cancellationToken);
        if (review is null || review.ConsultantId != consultantId)
            return Fail("Відгук не знайдено.");
        if (review.ConsultantReply is not null)
            return Fail("Ви вже відповіли на цей відгук.");

        var text = (request.Text ?? string.Empty).Trim();
        if (text.Length == 0 || text.Length > MaxReplyLength)
            return Fail($"Відповідь має містити від 1 до {MaxReplyLength} символів.");

        review.Reply(text, _clock.UtcNow);
        await _reviews.UpdateAsync(review, cancellationToken);

        await _notifications.NotifyAsync(
            review.ClientId,
            "Таролог відповів на ваш відгук",
            text,
            null,
            cancellationToken);

        return Result.Success(await ToDtoAsync(review, cancellationToken));
    }

    public async Task<Result<PagedResult<ReviewDto>>> ListForConsultantAsync(string consultantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return Result.Failure<PagedResult<ReviewDto>>("Номер сторінки має бути 1 або більше.");
        if (pageSize < 1 || pageSize > MaxPageSize)
            return Result.Failure<PagedResult<ReviewDto>>($"Розмір сторінки: від 1 до {MaxPageSize}.");

        var (items, total) = await _reviews.GetPageByConsultantAsync(consultantId, page, pageSize, cancellationToken);

        var dtos = new List<ReviewDto>();
        foreach (var review in items)
            dtos.Add(await ToDtoAsync(review, cancellationToken));

        return Result.Success(new PagedResult<ReviewDto>(dtos, total, page, pageSize));
    }

    // ---------- Рейтинг ----------

    /// <summary>Перераховує середню оцінку за всіма відгуками таролога.</summary>
    private async Task RecalculateRatingAsync(string consultantId, CancellationToken cancellationToken)
    {
        var all = await _reviews.GetAllByConsultantAsync(consultantId, cancellationToken);
        var count = all.Count;
        var average = count == 0 ? 0 : Math.Round(all.Average(r => (double)r.Rating), 2);

        if (await _users.GetByIdAsync(consultantId, cancellationToken) is Consultant consultant)
        {
            consultant.SetRating(average, count);
            await _users.UpdateAsync(consultant, cancellationToken);
        }
    }

    // ---------- Допоміжні ----------

    private static Result<ReviewDto> Fail(string error) => Result.Failure<ReviewDto>(error);

    private static bool IsValidRating(int rating) => rating >= 1 && rating <= 5;

    private static (string? Value, string? Error) NormalizeText(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null);

        var value = text.Trim();
        if (value.Length > maxLength)
            return (null, $"Текст відгуку не може бути довшим за {maxLength} символів.");

        return (value, null);
    }

    private async Task<ReviewDto> ToDtoAsync(Review review, CancellationToken cancellationToken)
    {
        var client = await _users.GetByIdAsync(review.ClientId, cancellationToken);

        return new ReviewDto(
            review.Id,
            review.ConsultationId,
            client?.DisplayName ?? "Клієнт",
            review.Rating,
            review.Text,
            review.CreatedAt,
            review.UpdatedAt,
            review.ConsultantReply,
            review.RepliedAt);
    }
}

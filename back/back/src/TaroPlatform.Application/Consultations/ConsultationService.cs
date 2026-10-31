using TaroPlatform.Application.Chat;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Application.Notifications;
using TaroPlatform.Domain.Consultations;
using TaroPlatform.Domain.Schedule;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Consultations;

public interface IConsultationService
{
    Task<Result<ConsultationDto>> BookAsync(string clientId, BookConsultationRequest request, CancellationToken cancellationToken = default);
    Task<Result<ConsultationDto>> ConfirmAsync(string consultantId, string consultationId, ConfirmConsultationRequest request, CancellationToken cancellationToken = default);
    Task<Result<ConsultationDto>> DeclineAsync(string consultantId, string consultationId, DeclineConsultationRequest request, CancellationToken cancellationToken = default);
    Task<Result<ConsultationDto>> CancelAsync(string userId, string consultationId, CancellationToken cancellationToken = default);
    Task<Result<ConsultationDto>> CompleteAsync(string consultantId, string consultationId, CancellationToken cancellationToken = default);
    Task<Result<ConsultationDto>> GetAsync(string userId, string consultationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultationDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default);
}

public class ConsultationService : IConsultationService
{
    private const string DefaultTimezone = "Europe/Kyiv";
    private const int MinLeadMinutes = 60;
    private const int MinQuestionLength = 10;
    private const int MaxQuestionLength = 1000;
    private const int MaxReasonLength = 300;

    private readonly IConsultationRepository _consultations;
    private readonly IUserRepository _users;
    private readonly IChatService _chat;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _clock;

    public ConsultationService(
        IConsultationRepository consultations,
        IUserRepository users,
        IChatService chat,
        INotificationService notifications,
        IDateTimeProvider clock)
    {
        _consultations = consultations;
        _users = users;
        _chat = chat;
        _notifications = notifications;
        _clock = clock;
    }

    /// <summary>Клієнт записується на вільний слот. Заявка одразу в статусі Pending.</summary>
    public async Task<Result<ConsultationDto>> BookAsync(string clientId, BookConsultationRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        var client = await _users.GetByIdAsync(clientId, cancellationToken);
        if (client is not Client)
            return Fail("Записуватися можуть лише клієнти.");

        if (await _users.GetByIdAsync(request.ConsultantId, cancellationToken) is not Consultant consultant)
            return Fail("Таролога не знайдено.");
        if (!consultant.IsAcceptingClients)
            return Fail("Таролог зараз не приймає клієнтів.");

        var question = (request.Question ?? string.Empty).Trim();
        if (question.Length < MinQuestionLength || question.Length > MaxQuestionLength)
            return Fail($"Опишіть ситуацію: від {MinQuestionLength} до {MaxQuestionLength} символів.");

        var startUtc = request.StartUtc.UtcDateTime;
        if (startUtc < now.AddMinutes(MinLeadMinutes))
            return Fail($"Записатися можна щонайменше за {MinLeadMinutes} хвилин до початку.");
        if (!IsOfferedSlot(consultant, startUtc))
            return Fail("Цей час недоступний у графіку таролога.");
        if (await _consultations.HasActiveAtAsync(consultant.Id, startUtc, cancellationToken))
            return Fail("Цей час уже зайнятий.");

        var consultation = new Consultation(
            clientId,
            consultant.Id,
            startUtc,
            consultant.SessionMinutes,
            request.Topic,
            question);
        await _consultations.AddAsync(consultation, cancellationToken);

        var when = FormatLocal(startUtc, consultant.Timezone ?? DefaultTimezone);
        await _notifications.NotifyAsync(
            consultant.Id,
            "Нова заявка на консультацію",
            $"{client.DisplayName} хоче записатися на {when}.",
            ConsultationLink(consultation.Id),
            cancellationToken);

        return Result.Success(await ToDtoAsync(consultation, cancellationToken));
    }

    /// <summary>
    /// Таролог підтверджує заявку. Клієнт отримує сповіщення і повідомлення в чаті з посиланням.
    /// </summary>
    public async Task<Result<ConsultationDto>> ConfirmAsync(string consultantId, string consultationId, ConfirmConsultationRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        var consultation = await _consultations.GetByIdAsync(consultationId, cancellationToken);
        if (consultation is null || consultation.ConsultantId != consultantId)
            return Fail("Заявку не знайдено.");
        if (consultation.Status != ConsultationStatus.Pending)
            return Fail("Заявку вже оброблено.");
        if (consultation.StartUtc <= now)
            return Fail("Час консультації вже минув.");

        var meetingUrl = NormalizeUrl(request.MeetingUrl);
        if (request.MeetingUrl is not null && meetingUrl is null)
            return Fail("Посилання на зустріч має починатися з http:// або https://.");

        consultation.Confirm(meetingUrl, now);

        var conversationId = await _chat.OpenConversationAsync(consultation.ClientId, consultation.ConsultantId, cancellationToken);
        consultation.AttachConversation(conversationId);
        await _consultations.UpdateAsync(consultation, cancellationToken);

        var consultant = await _users.GetByIdAsync(consultantId, cancellationToken) as Consultant;
        var when = FormatLocal(consultation.StartUtc, consultant?.Timezone ?? DefaultTimezone);
        var link = ConsultationLink(consultation.Id);

        var text = $"Консультацію підтверджено: {when}. Посилання на консультацію: {link}";
        if (meetingUrl is not null)
            text += $"\nПосилання на зустріч: {meetingUrl}";

        await _chat.PostSystemMessageAsync(conversationId, text, link, cancellationToken);
        await _notifications.NotifyAsync(consultation.ClientId, "Консультацію підтверджено", text, link, cancellationToken);

        return Result.Success(await ToDtoAsync(consultation, cancellationToken));
    }

    public async Task<Result<ConsultationDto>> DeclineAsync(string consultantId, string consultationId, DeclineConsultationRequest request, CancellationToken cancellationToken = default)
    {
        var consultation = await _consultations.GetByIdAsync(consultationId, cancellationToken);
        if (consultation is null || consultation.ConsultantId != consultantId)
            return Fail("Заявку не знайдено.");
        if (consultation.Status != ConsultationStatus.Pending)
            return Fail("Заявку вже оброблено.");

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason is not null && reason.Length > MaxReasonLength)
            return Fail($"Причина не може бути довшою за {MaxReasonLength} символів.");

        consultation.Decline(reason, _clock.UtcNow);
        await _consultations.UpdateAsync(consultation, cancellationToken);

        var body = reason is null
            ? "Таролог не може прийняти цю заявку."
            : $"Таролог не може прийняти заявку: {reason}";
        await _notifications.NotifyAsync(consultation.ClientId, "Заявку відхилено", body, ConsultationLink(consultation.Id), cancellationToken);

        return Result.Success(await ToDtoAsync(consultation, cancellationToken));
    }

    /// <summary>Скасування клієнтом або таро-консультантом. Інша сторона отримує сповіщення.</summary>
    public async Task<Result<ConsultationDto>> CancelAsync(string userId, string consultationId, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        var consultation = await _consultations.GetByIdAsync(consultationId, cancellationToken);
        if (consultation is null || !IsParticipant(consultation, userId))
            return Fail("Консультацію не знайдено.");
        if (!consultation.IsActive())
            return Fail("Консультацію вже не можна скасувати.");
        if (consultation.StartUtc <= now)
            return Fail("Консультація вже почалася або минула.");

        var byClient = consultation.ClientId == userId;
        if (byClient)
            consultation.CancelByClient(now);
        else
            consultation.CancelByConsultant(now);

        await _consultations.UpdateAsync(consultation, cancellationToken);

        var otherUserId = byClient ? consultation.ConsultantId : consultation.ClientId;
        var text = byClient
            ? "Клієнт скасував консультацію."
            : "Таролог скасував консультацію.";
        var link = ConsultationLink(consultation.Id);

        await _notifications.NotifyAsync(otherUserId, "Консультацію скасовано", text, link, cancellationToken);
        if (consultation.ConversationId is not null)
            await _chat.PostSystemMessageAsync(consultation.ConversationId, text, link, cancellationToken);

        return Result.Success(await ToDtoAsync(consultation, cancellationToken));
    }

    /// <summary>Таролог позначає консультацію проведеною після її початку.</summary>
    public async Task<Result<ConsultationDto>> CompleteAsync(string consultantId, string consultationId, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        var consultation = await _consultations.GetByIdAsync(consultationId, cancellationToken);
        if (consultation is null || consultation.ConsultantId != consultantId)
            return Fail("Консультацію не знайдено.");
        if (consultation.Status != ConsultationStatus.Confirmed)
            return Fail("Позначити проведеною можна лише підтверджену консультацію.");
        if (consultation.StartUtc > now)
            return Fail("Консультацію можна позначити проведеною після її початку.");

        consultation.Complete(now);
        await _consultations.UpdateAsync(consultation, cancellationToken);

        return Result.Success(await ToDtoAsync(consultation, cancellationToken));
    }

    public async Task<Result<ConsultationDto>> GetAsync(string userId, string consultationId, CancellationToken cancellationToken = default)
    {
        var consultation = await _consultations.GetByIdAsync(consultationId, cancellationToken);
        if (consultation is null || !IsParticipant(consultation, userId))
            return Fail("Консультацію не знайдено.");

        return Result.Success(await ToDtoAsync(consultation, cancellationToken));
    }

    public async Task<IReadOnlyList<ConsultationDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        var consultations = await _consultations.GetByParticipantAsync(userId, cancellationToken);
        var result = new List<ConsultationDto>();

        foreach (var consultation in consultations)
            result.Add(await ToDtoAsync(consultation, cancellationToken));

        return result;
    }

    // ---------- Допоміжні ----------

    private static Result<ConsultationDto> Fail(string error) => Result.Failure<ConsultationDto>(error);

    private static bool IsParticipant(Consultation consultation, string userId) =>
        consultation.ClientId == userId || consultation.ConsultantId == userId;

    private static string ConsultationLink(string consultationId) => $"/consultations/{consultationId}";

    /// <summary>Чи збігається час з початком слоту в графіку таролога (з урахуванням його часового поясу).</summary>
    private static bool IsOfferedSlot(Consultant consultant, DateTime startUtc)
    {
        var timezone = ResolveTimezone(consultant.Timezone ?? DefaultTimezone);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), timezone);

        if (local.Second != 0 || local.Minute % 5 != 0)
            return false;

        var minute = local.Hour * 60 + local.Minute;
        var starts = WorkingInterval.GenerateSlotStarts(consultant.Schedule, local.DayOfWeek, consultant.SessionMinutes);
        return starts.Contains(minute);
    }

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var value = url.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http"))
            return value;

        return null;
    }

    private static string FormatLocal(DateTime utc, string timezoneId)
    {
        var timezone = ResolveTimezone(timezoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timezone);
        return local.ToString("dd.MM.yyyy HH:mm");
    }

    private static TimeZoneInfo ResolveTimezone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private async Task<ConsultationDto> ToDtoAsync(Consultation consultation, CancellationToken cancellationToken)
    {
        var client = await _users.GetByIdAsync(consultation.ClientId, cancellationToken);
        var consultant = await _users.GetByIdAsync(consultation.ConsultantId, cancellationToken);

        return new ConsultationDto(
            consultation.Id,
            consultation.Status.ToString(),
            consultation.ClientId,
            client?.DisplayName ?? "Клієнт",
            consultation.ConsultantId,
            consultant?.DisplayName ?? "Таролог",
            new DateTimeOffset(DateTime.SpecifyKind(consultation.StartUtc, DateTimeKind.Utc), TimeSpan.Zero),
            consultation.DurationMinutes,
            consultation.Topic?.ToString(),
            consultation.Question,
            consultation.MeetingUrl,
            consultation.DeclineReason,
            consultation.ConversationId,
            ConsultationLink(consultation.Id),
            consultation.CreatedAt);
    }
}

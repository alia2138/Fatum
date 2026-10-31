using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Application.Consultations;

/// <summary>Клієнт обирає таролога, час зі слотів (UTC), тему і опис ситуації.</summary>
public record BookConsultationRequest(
    string ConsultantId,
    DateTimeOffset StartUtc,
    ReadingTopic? Topic,
    string Question);

/// <summary>MeetingUrl необов'язковий: посилання на відеозв'язок (Zoom, Google Meet тощо).</summary>
public record ConfirmConsultationRequest(string? MeetingUrl);

public record DeclineConsultationRequest(string? Reason);

/// <summary>
/// Заявка. Link — відносний шлях на сторінку консультації, фронт додає свою адресу.
/// </summary>
public record ConsultationDto(
    string Id,
    string Status,
    string ClientId,
    string ClientName,
    string ConsultantId,
    string ConsultantName,
    DateTimeOffset StartUtc,
    int DurationMinutes,
    string? Topic,
    string Question,
    string? MeetingUrl,
    string? DeclineReason,
    string? ConversationId,
    string Link,
    DateTime CreatedAt);

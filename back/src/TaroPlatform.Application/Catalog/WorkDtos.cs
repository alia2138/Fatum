namespace TaroPlatform.Application.Catalog;

/// <summary>ValidTo = null означає, що графік діє зараз.</summary>
public record ScheduleSnapshotDto(
    DateTime ValidFrom,
    DateTime? ValidTo,
    string Timezone,
    IReadOnlyList<ScheduleIntervalDto> Intervals);

/// <summary>
/// Звіт за період. Available — години, які таролог заклав у графік.
/// Completed — години проведених консультацій. Booked — підтверджені і проведені.
/// </summary>
public record WorkSummaryDto(
    DateOnly From,
    DateOnly To,
    double AvailableHours,
    double BookedHours,
    double CompletedHours,
    int CompletedCount,
    int ConfirmedCount,
    int CancelledCount,
    int DeclinedCount);

public record ConsultantConsultationDto(
    string Id,
    string Status,
    string ClientId,
    string ClientName,
    DateTimeOffset StartUtc,
    int DurationMinutes,
    string? Topic,
    string Question,
    string? MeetingUrl,
    string? ConversationId);

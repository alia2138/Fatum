using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Consultations;
using TaroPlatform.Domain.Schedule;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Catalog;

public interface IConsultantWorkService
{
    Task<Result<IReadOnlyList<ScheduleSnapshotDto>>> GetScheduleHistoryAsync(string consultantId, CancellationToken cancellationToken = default);
    Task<Result<WorkSummaryDto>> GetWorkSummaryAsync(string consultantId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ConsultantConsultationDto>>> GetConsultationsAsync(string consultantId, DateOnly? from, DateOnly? to, ConsultationStatus? status, CancellationToken cancellationToken = default);
}

/// <summary>Історія графіків, звіт про години роботи і список консультацій таролога за період.</summary>
public class ConsultantWorkService : IConsultantWorkService
{
    private const int MaxRangeDays = 366;
    private const string DefaultTimezone = "Europe/Kyiv";

    private readonly IUserRepository _users;
    private readonly IConsultationRepository _consultations;

    public ConsultantWorkService(IUserRepository users, IConsultationRepository consultations)
    {
        _users = users;
        _consultations = consultations;
    }

    public async Task<Result<IReadOnlyList<ScheduleSnapshotDto>>> GetScheduleHistoryAsync(string consultantId, CancellationToken cancellationToken = default)
    {
        if (await _users.GetByIdAsync(consultantId, cancellationToken) is not Consultant consultant)
            return Result.Failure<IReadOnlyList<ScheduleSnapshotDto>>("Таролога не знайдено.");

        var result = new List<ScheduleSnapshotDto>();

        if (consultant.Schedule.Count > 0)
        {
            result.Add(new ScheduleSnapshotDto(
                consultant.ScheduleChangedAt,
                null,
                consultant.Timezone ?? DefaultTimezone,
                ToDto(consultant.Schedule)));
        }

        foreach (var snapshot in consultant.ScheduleHistory.OrderByDescending(s => s.ValidFrom))
        {
            result.Add(new ScheduleSnapshotDto(
                snapshot.ValidFrom,
                snapshot.ValidTo,
                snapshot.Timezone ?? DefaultTimezone,
                ToDto(snapshot.Intervals)));
        }

        return Result.Success<IReadOnlyList<ScheduleSnapshotDto>>(result);
    }

    public async Task<Result<WorkSummaryDto>> GetWorkSummaryAsync(string consultantId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (from > to)
            return Result.Failure<WorkSummaryDto>("Дата початку пізніша за дату кінця.");
        if (to.DayNumber - from.DayNumber > MaxRangeDays)
            return Result.Failure<WorkSummaryDto>($"Період не може бути довшим за {MaxRangeDays} днів.");
        if (await _users.GetByIdAsync(consultantId, cancellationToken) is not Consultant consultant)
            return Result.Failure<WorkSummaryDto>("Таролога не знайдено.");

        // Доступний час: скільки годин графік відкривав у кожен день періоду
        var availableMinutes = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var interval in IntervalsOn(consultant, date).Where(i => i.Day == date.DayOfWeek))
                availableMinutes += interval.EndMinute - interval.StartMinute;
        }

        // Консультації, що починаються в періоді
        var mine = (await _consultations.GetByParticipantAsync(consultantId, cancellationToken))
            .Where(c => c.ConsultantId == consultantId)
            .Where(c => DateOnly.FromDateTime(c.StartUtc) >= from && DateOnly.FromDateTime(c.StartUtc) <= to)
            .ToList();

        var completed = mine.Where(c => c.Status == ConsultationStatus.Completed).ToList();
        var confirmed = mine.Where(c => c.Status == ConsultationStatus.Confirmed).ToList();
        var booked = completed.Concat(confirmed).Sum(c => c.DurationMinutes);

        return Result.Success(new WorkSummaryDto(
            from,
            to,
            Math.Round(availableMinutes / 60.0, 2),
            Math.Round(booked / 60.0, 2),
            Math.Round(completed.Sum(c => c.DurationMinutes) / 60.0, 2),
            completed.Count,
            confirmed.Count,
            mine.Count(c => c.Status == ConsultationStatus.CancelledByClient || c.Status == ConsultationStatus.CancelledByConsultant),
            mine.Count(c => c.Status == ConsultationStatus.Declined)));
    }

    public async Task<Result<IReadOnlyList<ConsultantConsultationDto>>> GetConsultationsAsync(string consultantId, DateOnly? from, DateOnly? to, ConsultationStatus? status, CancellationToken cancellationToken = default)
    {
        if (from is not null && to is not null && from > to)
            return Result.Failure<IReadOnlyList<ConsultantConsultationDto>>("Дата початку пізніша за дату кінця.");

        var items = (await _consultations.GetByParticipantAsync(consultantId, cancellationToken))
            .Where(c => c.ConsultantId == consultantId)
            .Where(c => from is null || DateOnly.FromDateTime(c.StartUtc) >= from)
            .Where(c => to is null || DateOnly.FromDateTime(c.StartUtc) <= to)
            .Where(c => status is null || c.Status == status)
            .OrderByDescending(c => c.StartUtc)
            .ToList();

        var result = new List<ConsultantConsultationDto>();
        foreach (var c in items)
        {
            var client = await _users.GetByIdAsync(c.ClientId, cancellationToken);
            result.Add(new ConsultantConsultationDto(
                c.Id,
                c.Status.ToString(),
                c.ClientId,
                client?.DisplayName ?? "Клієнт",
                new DateTimeOffset(DateTime.SpecifyKind(c.StartUtc, DateTimeKind.Utc), TimeSpan.Zero),
                c.DurationMinutes,
                c.Topic?.ToString(),
                c.Question,
                c.MeetingUrl,
                c.ConversationId));
        }

        return Result.Success<IReadOnlyList<ConsultantConsultationDto>>(result);
    }

    /// <summary>Графік, що діяв у цю дату: з історії або поточний.</summary>
    private static IReadOnlyList<WorkingInterval> IntervalsOn(Consultant consultant, DateOnly date)
    {
        var day = date.ToDateTime(TimeOnly.MinValue);

        var snapshot = consultant.ScheduleHistory.FirstOrDefault(s => day >= s.ValidFrom.Date && day < s.ValidTo.Date);
        if (snapshot is not null)
            return snapshot.Intervals;

        return day >= consultant.ScheduleChangedAt.Date ? consultant.Schedule : Array.Empty<WorkingInterval>();
    }

    private static List<ScheduleIntervalDto> ToDto(IEnumerable<WorkingInterval> intervals) =>
        intervals
            .OrderBy(i => i.Day)
            .ThenBy(i => i.StartMinute)
            .Select(i => new ScheduleIntervalDto(i.Day.ToString(), FormatMinute(i.StartMinute), FormatMinute(i.EndMinute)))
            .ToList();

    private static string FormatMinute(int minute) =>
        minute == 1440 ? "24:00" : $"{minute / 60:00}:{minute % 60:00}";
}

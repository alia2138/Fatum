using System.Globalization;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Schedule;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Catalog;

public interface IConsultantService
{
    Task<Result<PagedResult<ConsultantCardDto>>> SearchAsync(ConsultantSearch search, CancellationToken cancellationToken = default);
    Task<Result<ConsultantPublicDto>> GetPublicProfileAsync(string consultantId, CancellationToken cancellationToken = default);
    Task<Result<ScheduleDto>> GetScheduleAsync(string consultantId, CancellationToken cancellationToken = default);
    Task<Result<ScheduleDto>> UpdateScheduleAsync(string consultantUserId, UpdateScheduleRequest request, CancellationToken cancellationToken = default);
    Task<Result<SlotsDto>> GetSlotsAsync(string consultantId, DateOnly date, CancellationToken cancellationToken = default);
}

public class ConsultantService : IConsultantService
{
    private const string DefaultTimezone = "Europe/Kyiv";
    private const int MaxPageSize = 50;
    private const int MaxIntervals = 21;
    private const int MaxPreviewLength = 160;
    private static readonly string[] SortFields = { "price", "experience", "name", "rating" };

    private readonly IUserRepository _users;

    public ConsultantService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<Result<PagedResult<ConsultantCardDto>>> SearchAsync(ConsultantSearch search, CancellationToken cancellationToken = default)
    {
        if (search.Page < 1)
            return Result.Failure<PagedResult<ConsultantCardDto>>("Номер сторінки має бути 1 або більше.");
        if (search.PageSize < 1 || search.PageSize > MaxPageSize)
            return Result.Failure<PagedResult<ConsultantCardDto>>($"Розмір сторінки: від 1 до {MaxPageSize}.");
        if (search.MinPrice > search.MaxPrice)
            return Result.Failure<PagedResult<ConsultantCardDto>>("Мінімальна ціна більша за максимальну.");
        if (!SortFields.Contains(search.SortBy))
            return Result.Failure<PagedResult<ConsultantCardDto>>("Сортування: price, experience або name.");

        var (items, total) = await _users.SearchConsultantsAsync(search, cancellationToken);
        var cards = items.Select(ToCard).ToList();

        return Result.Success(new PagedResult<ConsultantCardDto>(cards, total, search.Page, search.PageSize));
    }

    public async Task<Result<ConsultantPublicDto>> GetPublicProfileAsync(string consultantId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(consultantId, cancellationToken);
        if (user is not Consultant consultant)
            return Result.Failure<ConsultantPublicDto>("Таролога не знайдено.");

        return Result.Success(new ConsultantPublicDto(
            consultant.Id,
            consultant.DisplayName,
            consultant.PhotoUrl,
            consultant.Bio,
            consultant.Specializations.Select(t => t.ToString()).ToList(),
            consultant.ExperienceYears,
            consultant.Languages,
            consultant.PriceUah,
            consultant.SessionMinutes,
            consultant.Timezone ?? DefaultTimezone,
            consultant.IsAcceptingClients,
            RatingAverage: RatingOf(consultant),
            ReviewsCount: consultant.ReviewsCount));
    }

    public async Task<Result<ScheduleDto>> GetScheduleAsync(string consultantId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(consultantId, cancellationToken);
        if (user is not Consultant consultant)
            return Result.Failure<ScheduleDto>("Таролога не знайдено.");

        return Result.Success(ToSchedule(consultant));
    }

    public async Task<Result<ScheduleDto>> UpdateScheduleAsync(string consultantUserId, UpdateScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(consultantUserId, cancellationToken);
        if (user is null)
            return Result.Failure<ScheduleDto>("Користувача не знайдено.");
        if (user is not Consultant consultant)
            return Result.Failure<ScheduleDto>("Графік доступний лише таро-консультантам.");

        if (string.IsNullOrWhiteSpace(request.Timezone))
            return Result.Failure<ScheduleDto>("Вкажіть часовий пояс, наприклад Europe/Kyiv.");
        if (!IsValidTimezone(request.Timezone))
            return Result.Failure<ScheduleDto>("Невірний часовий пояс, наприклад Europe/Kyiv.");

        if (request.Intervals.Count > MaxIntervals)
            return Result.Failure<ScheduleDto>($"Не більше {MaxIntervals} інтервалів на тиждень.");

        var intervals = new List<WorkingInterval>();
        foreach (var item in request.Intervals)
        {
            if (!Enum.TryParse<DayOfWeek>(item.Day, ignoreCase: true, out var day))
                return Result.Failure<ScheduleDto>($"Невірний день: {item.Day}.");
            if (!TryParseMinute(item.Start, out var start) || !TryParseMinute(item.End, out var end))
                return Result.Failure<ScheduleDto>("Час має формат HH:mm, наприклад 09:00.");
            if (start >= end)
                return Result.Failure<ScheduleDto>("Початок інтервалу має бути раніше за кінець.");
            if (start % 5 != 0 || end % 5 != 0)
                return Result.Failure<ScheduleDto>("Час має бути кратним 5 хвилинам.");

            intervals.Add(new WorkingInterval(day, start, end));
        }

        // Інтервали в один день не можуть перетинатися
        foreach (var group in intervals.GroupBy(i => i.Day))
        {
            var ordered = group.OrderBy(i => i.StartMinute).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartMinute < ordered[i - 1].EndMinute)
                    return Result.Failure<ScheduleDto>($"Інтервали в день {group.Key} перетинаються.");
            }
        }

        consultant.SetTimezone(request.Timezone.Trim());
        consultant.SetSchedule(intervals);

        await _users.UpdateAsync(consultant, cancellationToken);
        return Result.Success(ToSchedule(consultant));
    }

    /// <summary>
    /// Вільні слоти на дату в часовому поясі таролога. Зайняті слоти (бронювання)
    /// додамо на наступному етапі, тут для них буде окремий фільтр.
    /// </summary>
    public async Task<Result<SlotsDto>> GetSlotsAsync(string consultantId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(consultantId, cancellationToken);
        if (user is not Consultant consultant)
            return Result.Failure<SlotsDto>("Таролога не знайдено.");
        if (!consultant.IsAcceptingClients)
            return Result.Failure<SlotsDto>("Таролог зараз не приймає клієнтів.");

        var timezoneId = consultant.Timezone ?? DefaultTimezone;
        var timezone = ResolveTimezone(timezoneId);

        var starts = WorkingInterval.GenerateSlotStarts(consultant.Schedule, date.DayOfWeek, consultant.SessionMinutes);
        var slots = new List<SlotDto>();

        foreach (var minute in starts)
        {
            var local = date.ToDateTime(TimeOnly.MinValue).AddMinutes(minute);
            DateTime utc;
            try
            {
                utc = TimeZoneInfo.ConvertTimeToUtc(local, timezone);
            }
            catch (ArgumentException)
            {
                // Час, якого не існує через перехід на літній час, пропускаємо
                continue;
            }

            slots.Add(new SlotDto(
                FormatMinute(minute),
                new DateTimeOffset(utc, TimeSpan.Zero),
                consultant.SessionMinutes));
        }

        return Result.Success(new SlotsDto(date, timezoneId, slots));
    }

    // ---------- Допоміжні ----------

    private static ConsultantCardDto ToCard(Consultant consultant) =>
        new(
            consultant.Id,
            consultant.DisplayName,
            consultant.PhotoUrl,
            BioPreview(consultant.Bio),
            consultant.Specializations.Select(t => t.ToString()).ToList(),
            consultant.ExperienceYears,
            consultant.Languages,
            consultant.PriceUah,
            consultant.SessionMinutes,
            RatingAverage: RatingOf(consultant),
            ReviewsCount: consultant.ReviewsCount);

    private static ScheduleDto ToSchedule(Consultant consultant)
    {
        var intervals = consultant.Schedule
            .OrderBy(i => i.Day)
            .ThenBy(i => i.StartMinute)
            .Select(i => new ScheduleIntervalDto(i.Day.ToString(), FormatMinute(i.StartMinute), FormatMinute(i.EndMinute)))
            .ToList();

        return new ScheduleDto(consultant.Timezone ?? DefaultTimezone, intervals);
    }

    /// <summary>Рейтинг показуємо лише якщо є хоча б один відгук.</summary>
    private static double? RatingOf(Consultant consultant) =>
        consultant.ReviewsCount > 0 ? consultant.RatingAverage : null;

    private static string? BioPreview(string? bio)
    {
        if (string.IsNullOrWhiteSpace(bio))
            return null;

        return bio.Length <= MaxPreviewLength ? bio : bio[..MaxPreviewLength] + "…";
    }

    private static bool TryParseMinute(string value, out int minute)
    {
        if (value == "24:00")
        {
            minute = 1440;
            return true;
        }

        if (TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            minute = time.Hour * 60 + time.Minute;
            return true;
        }

        minute = 0;
        return false;
    }

    private static string FormatMinute(int minute) =>
        minute == 1440 ? "24:00" : $"{minute / 60:00}:{minute % 60:00}";

    private static bool IsValidTimezone(string id)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(id.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
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
}

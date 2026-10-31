using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Application.Catalog;

/// <summary>Параметри пошуку. Усі фільтри необов'язкові.</summary>
public record ConsultantSearch(
    string? Text,
    ReadingTopic? Topic,
    string? Language,
    int? MinPrice,
    int? MaxPrice,
    int? MinExperience,
    string SortBy,
    bool Descending,
    int Page,
    int PageSize);

/// <summary>Картка в каталозі. Рейтинг поки null, заповниться після відгуків.</summary>
public record ConsultantCardDto(
    string Id,
    string DisplayName,
    string? PhotoUrl,
    string? BioPreview,
    IReadOnlyList<string> Specializations,
    int ExperienceYears,
    IReadOnlyList<string> Languages,
    int PriceUah,
    int SessionMinutes,
    double? RatingAverage,
    int ReviewsCount);

/// <summary>Повний публічний профіль. Telegram-нік сюди не потрапляє.</summary>
public record ConsultantPublicDto(
    string Id,
    string DisplayName,
    string? PhotoUrl,
    string? Bio,
    IReadOnlyList<string> Specializations,
    int ExperienceYears,
    IReadOnlyList<string> Languages,
    int PriceUah,
    int SessionMinutes,
    string Timezone,
    bool IsAcceptingClients,
    double? RatingAverage,
    int ReviewsCount);

public record PagedResult<T>(IReadOnlyList<T> Items, long Total, int Page, int PageSize);

/// <summary>Інтервал графіка. Day: Monday..Sunday, час: "HH:mm" (24:00 для кінця доби).</summary>
public record ScheduleIntervalDto(string Day, string Start, string End);

public record ScheduleDto(string Timezone, IReadOnlyList<ScheduleIntervalDto> Intervals);

/// <summary>Повна заміна тижневого графіка.</summary>
public record UpdateScheduleRequest(string Timezone, List<ScheduleIntervalDto> Intervals);

public record SlotDto(string StartLocal, DateTimeOffset StartUtc, int DurationMinutes);

public record SlotsDto(DateOnly Date, string Timezone, IReadOnlyList<SlotDto> Slots);

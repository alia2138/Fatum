using TaroPlatform.Domain.Enums;
using TaroPlatform.Domain.Schedule;
using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Domain.Users;

/// <summary>
/// Таролог: профіль, графік, прийом клієнтів і рейтинг за відгуками.
/// </summary>
public class Consultant : User
{
    protected Consultant() { }

    public Consultant(string email, string passwordHash)
        : base(email, passwordHash, UserRole.Consultant)
    {
        SessionMinutes = 60;
    }

    /// <summary>Теми консультацій (ті самі, що в гаданнях).</summary>
    public List<ReadingTopic> Specializations { get; private set; } = new();
    public int ExperienceYears { get; private set; }
    public List<string> Languages { get; private set; } = new();
    public int PriceUah { get; private set; }
    public int SessionMinutes { get; private set; }
    public bool IsAcceptingClients { get; private set; }
    public string? ContactTelegram { get; private set; }

    /// <summary>Часовий пояс, у якому задано графік. null означає Europe/Kyiv.</summary>
    public string? Timezone { get; private set; }

    /// <summary>Тижневий графік: кілька інтервалів на різні дні.</summary>
    public List<WorkingInterval> Schedule { get; private set; } = new();

    /// <summary>Середня оцінка (1–5), перераховується після кожного відгуку.</summary>
    public double RatingAverage { get; private set; }

    /// <summary>Кількість відгуків.</summary>
    public int ReviewsCount { get; private set; }

    public void SetSpecializations(IEnumerable<ReadingTopic> topics) => Specializations = topics.Distinct().ToList();
    public void SetExperienceYears(int years) => ExperienceYears = years;
    public void SetLanguages(IEnumerable<string> languages) => Languages = languages.Distinct().ToList();
    public void SetPriceUah(int price) => PriceUah = price;
    public void SetSessionMinutes(int minutes) => SessionMinutes = minutes;
    public void SetContactTelegram(string? username) =>
        ContactTelegram = string.IsNullOrWhiteSpace(username) ? null : username;
    public void SetAcceptingClients(bool accepting) => IsAcceptingClients = accepting;
    public void SetTimezone(string? timezone) =>
        Timezone = string.IsNullOrWhiteSpace(timezone) ? null : timezone;
    /// <summary>Коли поточний графік вступив у силу (UTC).</summary>
    public DateTime ScheduleChangedAt { get; private set; }

    /// <summary>Попередні графіки. Кожна зміна графіка зберігає старий.</summary>
    public List<ScheduleSnapshot> ScheduleHistory { get; private set; } = new();

    /// <summary>Замінює графік і зберігає попередній у історію.</summary>
    public void ReplaceSchedule(string? timezone, IEnumerable<WorkingInterval> intervals, DateTime now)
    {
        if (Schedule.Count > 0)
            ScheduleHistory.Add(new ScheduleSnapshot(ScheduleChangedAt, now, Timezone, Schedule.ToList()));

        Timezone = string.IsNullOrWhiteSpace(timezone) ? null : timezone;
        Schedule = intervals.ToList();
        ScheduleChangedAt = now;
    }

    public void SetSchedule(IEnumerable<WorkingInterval> intervals) => Schedule = intervals.ToList();
    public void SetRating(double average, int count)
    {
        RatingAverage = average;
        ReviewsCount = count;
    }

    public override IReadOnlyList<string> GetMissingProfileFields()
    {
        var missing = base.GetMissingProfileFields().ToList();
        if (string.IsNullOrWhiteSpace(PhotoUrl))
            missing.Add("Фото");
        if (string.IsNullOrWhiteSpace(Bio))
            missing.Add("Опис про себе");
        if (Specializations.Count == 0)
            missing.Add("Спеціалізації");
        if (PriceUah <= 0)
            missing.Add("Ціна консультації");
        return missing;
    }
}

using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Notifications;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Reminders;

public interface IReminderService
{
    /// <summary>Надсилає нагадування про консультації, що починаються протягом години. Повертає кількість.</summary>
    Task<int> SendDueAsync(CancellationToken cancellationToken = default);
}

public class ReminderService : IReminderService
{
    private const int LeadMinutes = 60;
    private const string DefaultTimezone = "Europe/Kyiv";

    private readonly IConsultationRepository _consultations;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _clock;

    public ReminderService(
        IConsultationRepository consultations,
        IUserRepository users,
        INotificationService notifications,
        IDateTimeProvider clock)
    {
        _consultations = consultations;
        _users = users;
        _notifications = notifications;
        _clock = clock;
    }

    public async Task<int> SendDueAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var due = await _consultations.GetDueRemindersAsync(now, now.AddMinutes(LeadMinutes), cancellationToken);

        var sent = 0;
        foreach (var consultation in due)
        {
            var consultant = await _users.GetByIdAsync(consultation.ConsultantId, cancellationToken) as Consultant;
            var when = FormatLocal(consultation.StartUtc, consultant?.Timezone ?? DefaultTimezone);
            var link = $"/consultations/{consultation.Id}";
            var text = $"Консультація почнеться о {when}.";

            await _notifications.NotifyAsync(consultation.ClientId, "Нагадування про консультацію", text, link, cancellationToken);
            await _notifications.NotifyAsync(consultation.ConsultantId, "Нагадування про консультацію", text, link, cancellationToken);

            consultation.MarkReminderSent();
            await _consultations.UpdateAsync(consultation, cancellationToken);
            sent++;
        }

        return sent;
    }

    private static string FormatLocal(DateTime utc, string timezoneId)
    {
        TimeZoneInfo timezone;
        try
        {
            timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timezone = TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            timezone = TimeZoneInfo.Utc;
        }

        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timezone);
        return local.ToString("dd.MM.yyyy HH:mm");
    }
}

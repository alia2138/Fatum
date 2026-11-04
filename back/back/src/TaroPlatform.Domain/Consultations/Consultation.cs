using TaroPlatform.Domain.Common;
using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Domain.Consultations;

public enum ConsultationStatus
{
    Pending = 0,
    Confirmed = 1,
    Declined = 2,
    CancelledByClient = 3,
    CancelledByConsultant = 4,
    Completed = 5
}

/// <summary>
/// Заявка на консультацію. Статуси змінюються лише через методи, які перевіряють дозволені переходи.
/// </summary>
public class Consultation : Entity
{
    protected Consultation() { }

    public Consultation(string clientId, string consultantId, DateTime startUtc, int durationMinutes, ReadingTopic? topic, string question)
    {
        ClientId = clientId;
        ConsultantId = consultantId;
        StartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        DurationMinutes = durationMinutes;
        Topic = topic;
        Question = question;
        Status = ConsultationStatus.Pending;
    }

    public string ClientId { get; private set; } = string.Empty;
    public string ConsultantId { get; private set; } = string.Empty;
    public DateTime StartUtc { get; private set; }
    public int DurationMinutes { get; private set; }
    public ReadingTopic? Topic { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public ConsultationStatus Status { get; private set; }
    public string? MeetingUrl { get; private set; }
    public string? DeclineReason { get; private set; }
    public string? ConversationId { get; private set; }
    public DateTime? StatusChangedAt { get; private set; }

    /// <summary>Активна заявка займає слот: її не можна записати повторно.</summary>
    public bool IsActive() => Status == ConsultationStatus.Pending || Status == ConsultationStatus.Confirmed;

    public void Confirm(string? meetingUrl, DateTime now)
    {
        Require(ConsultationStatus.Pending);
        Status = ConsultationStatus.Confirmed;
        MeetingUrl = meetingUrl;
        StatusChangedAt = now;
    }

    public void Decline(string? reason, DateTime now)
    {
        Require(ConsultationStatus.Pending);
        Status = ConsultationStatus.Declined;
        DeclineReason = reason;
        StatusChangedAt = now;
    }

    public void CancelByClient(DateTime now)
    {
        if (!IsActive())
            throw new InvalidOperationException("Консультацію вже не можна скасувати.");
        Status = ConsultationStatus.CancelledByClient;
        StatusChangedAt = now;
    }

    public void CancelByConsultant(DateTime now)
    {
        if (!IsActive())
            throw new InvalidOperationException("Консультацію вже не можна скасувати.");
        Status = ConsultationStatus.CancelledByConsultant;
        StatusChangedAt = now;
    }

    public void Complete(DateTime now)
    {
        Require(ConsultationStatus.Confirmed);
        Status = ConsultationStatus.Completed;
        StatusChangedAt = now;
    }

    /// <summary>Чи вже надіслано нагадування за годину до консультації.</summary>
    public bool ReminderSent { get; private set; }

    public void MarkReminderSent() => ReminderSent = true;

    public void AttachConversation(string conversationId) => ConversationId = conversationId;

    private void Require(ConsultationStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Недопустима зміна статусу з {Status}.");
    }
}

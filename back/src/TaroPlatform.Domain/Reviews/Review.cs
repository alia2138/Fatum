using TaroPlatform.Domain.Common;

namespace TaroPlatform.Domain.Reviews;

/// <summary>
/// Відгук клієнта про проведену консультацію. Одна консультація — один відгук.
/// Таролог може відповісти один раз.
/// </summary>
public class Review : Entity
{
    protected Review() { }

    public Review(string consultationId, string clientId, string consultantId, int rating, string? text)
    {
        ConsultationId = consultationId;
        ClientId = clientId;
        ConsultantId = consultantId;
        Rating = rating;
        Text = text;
    }

    public string ConsultationId { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public string ConsultantId { get; private set; } = string.Empty;
    public int Rating { get; private set; }
    public string? Text { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? ConsultantReply { get; private set; }
    public DateTime? RepliedAt { get; private set; }

    public void Update(int rating, string? text, DateTime now)
    {
        Rating = rating;
        Text = text;
        UpdatedAt = now;
    }

    public void Reply(string text, DateTime now)
    {
        ConsultantReply = text;
        RepliedAt = now;
    }
}

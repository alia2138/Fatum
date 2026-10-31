using TaroPlatform.Domain.Common;

namespace TaroPlatform.Domain.Chat;

public enum MessageKind
{
    Text = 0,
    System = 1,
    ConsultationLink = 2
}

/// <summary>Повідомлення в чаті. SenderId порожній для системних повідомлень.</summary>
public class ChatMessage
{
    protected ChatMessage() { }

    public ChatMessage(string? senderId, MessageKind kind, string text, string? link)
    {
        SenderId = senderId;
        Kind = kind;
        Text = text;
        Link = link;
        CreatedAt = DateTime.UtcNow;
    }

    public string? SenderId { get; private set; }
    public MessageKind Kind { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public DateTime CreatedAt { get; private set; }
}

/// <summary>Чат між одним клієнтом і одним таро-консультантом.</summary>
public class Conversation : Entity
{
    protected Conversation() { }

    public Conversation(string clientId, string consultantId)
    {
        ClientId = clientId;
        ConsultantId = consultantId;
        LastMessageAt = CreatedAt;
    }

    public string ClientId { get; private set; } = string.Empty;
    public string ConsultantId { get; private set; } = string.Empty;
    public DateTime LastMessageAt { get; private set; }
    public List<ChatMessage> Messages { get; private set; } = new();

    public bool HasParticipant(string userId) => ClientId == userId || ConsultantId == userId;

    public void AddMessage(ChatMessage message)
    {
        Messages.Add(message);
        LastMessageAt = message.CreatedAt;
    }
}

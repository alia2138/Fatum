namespace TaroPlatform.Domain.Tarot;

/// <summary>
/// Одне повідомлення в діалозі гадання. Provider фіксує, хто згенерував відповідь:
/// "ai" (справжня модель) або "stub" (заглушка).
/// </summary>
public class ReadingMessage
{
    protected ReadingMessage() { }

    public ReadingMessage(MessageRole role, string text, string provider = "")
    {
        Role = role;
        Text = text;
        Provider = provider;
        CreatedAt = DateTime.UtcNow;
    }

    public MessageRole Role { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
}

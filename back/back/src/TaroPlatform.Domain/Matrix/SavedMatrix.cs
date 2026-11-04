using TaroPlatform.Domain.Common;

namespace TaroPlatform.Domain.Matrix;

public enum MatrixMessageRole
{
    User = 0,
    Assistant = 1
}

/// <summary>Повідомлення в розмові про матрицю. Zone — обрана зона, або null.</summary>
public class MatrixMessage
{
    protected MatrixMessage() { }

    public MatrixMessage(MatrixMessageRole role, string text, string? zone, string provider)
    {
        Role = role;
        Text = text;
        Zone = zone;
        Provider = provider;
        CreatedAt = DateTime.UtcNow;
    }

    public MatrixMessageRole Role { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string? Zone { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
}

/// <summary>
/// Збережена матриця: дата, спосіб і розмова з AI. Точки не зберігаються,
/// бо детерміновано перераховуються з дати і способу.
/// </summary>
public class SavedMatrix : Entity
{
    protected SavedMatrix() { }

    public SavedMatrix(string userId, DateTime birthDate, ChannelMethod method, string? label)
    {
        UserId = userId;
        BirthDate = birthDate.Date;
        Method = method;
        Label = label;
    }

    public string UserId { get; private set; } = string.Empty;
    public DateTime BirthDate { get; private set; }
    public ChannelMethod Method { get; private set; }
    public string? Label { get; private set; }
    public List<MatrixMessage> Messages { get; private set; } = new();

    public void SetLabel(string? label) => Label = label;

    public void ClearMessages() => Messages.Clear();

    public void AddMessage(MatrixMessageRole role, string text, string? zone = null, string provider = "") =>
        Messages.Add(new MatrixMessage(role, text, zone, provider));
}

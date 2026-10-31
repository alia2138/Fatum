using TaroPlatform.Domain.Common;

namespace TaroPlatform.Domain.Notifications;

/// <summary>Сповіщення всередині застосунку. Link веде на сторінку, яку стосується сповіщення.</summary>
public class Notification : Entity
{
    protected Notification() { }

    public Notification(string userId, string title, string body, string? link)
    {
        UserId = userId;
        Title = title;
        Body = body;
        Link = link;
    }

    public string UserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public bool IsRead { get; private set; }

    public void MarkRead() => IsRead = true;
}

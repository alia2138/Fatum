using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Notifications;

namespace TaroPlatform.Application.Notifications;

public record NotificationDto(string Id, string Title, string Body, string? Link, bool IsRead, DateTime CreatedAt);

public interface INotificationService
{
    Task NotifyAsync(string userId, string title, string body, string? link, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result> MarkReadAsync(string userId, string notificationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Сповіщення зберігаються в застосунку і одночасно надсилаються на email і Telegram, якщо вони є у користувача.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly IExternalNotifier _external;

    public NotificationService(INotificationRepository notifications, IUserRepository users, IExternalNotifier external)
    {
        _notifications = notifications;
        _users = users;
        _external = external;
    }

    public async Task NotifyAsync(string userId, string title, string body, string? link, CancellationToken cancellationToken = default)
    {
        await _notifications.AddAsync(new Notification(userId, title, body, link), cancellationToken);

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is not null)
            await _external.SendAsync(user, title, body, link, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await _notifications.GetByUserAsync(userId, cancellationToken);
        return items
            .Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.Link, n.IsRead, n.CreatedAt))
            .ToList();
    }

    public async Task<Result> MarkReadAsync(string userId, string notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _notifications.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null || notification.UserId != userId)
            return Result.Failure("Сповіщення не знайдено.");

        notification.MarkRead();
        await _notifications.UpdateAsync(notification, cancellationToken);
        return Result.Success();
    }
}

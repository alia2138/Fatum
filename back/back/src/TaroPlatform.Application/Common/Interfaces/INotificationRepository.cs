using TaroPlatform.Domain.Notifications;

namespace TaroPlatform.Application.Common.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
}

using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Notifications;

namespace TaroPlatform.Infrastructure.Persistence;

public class NotificationRepository : MongoRepository<Notification>, INotificationRepository
{
    public NotificationRepository(MongoContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Notification>> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(n => n.UserId == userId)
            .SortByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
}

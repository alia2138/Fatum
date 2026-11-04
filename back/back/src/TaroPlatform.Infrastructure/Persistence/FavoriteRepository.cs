using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Favorites;

namespace TaroPlatform.Infrastructure.Persistence;

public class FavoriteRepository : MongoRepository<Favorite>, IFavoriteRepository
{
    public FavoriteRepository(MongoContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Favorite>> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(f => f.UserId == userId)
            .SortByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Favorite?> FindAsync(string userId, FavoriteType type, string targetId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(f => f.UserId == userId && f.Type == type && f.TargetId == targetId)
            .FirstOrDefaultAsync(cancellationToken);
}

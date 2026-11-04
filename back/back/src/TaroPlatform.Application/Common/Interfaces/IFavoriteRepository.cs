using TaroPlatform.Domain.Favorites;

namespace TaroPlatform.Application.Common.Interfaces;

public interface IFavoriteRepository : IRepository<Favorite>
{
    Task<IReadOnlyList<Favorite>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<Favorite?> FindAsync(string userId, FavoriteType type, string targetId, CancellationToken cancellationToken = default);
}

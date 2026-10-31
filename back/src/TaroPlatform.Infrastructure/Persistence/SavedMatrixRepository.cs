using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Matrix;

namespace TaroPlatform.Infrastructure.Persistence;

public class SavedMatrixRepository : MongoRepository<SavedMatrix>, ISavedMatrixRepository
{
    public SavedMatrixRepository(MongoContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SavedMatrix>> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(m => m.UserId == userId)
            .SortByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<SavedMatrix?> GetOwnedAsync(string id, string userId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(m => m.Id == id && m.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
}

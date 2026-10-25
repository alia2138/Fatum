using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Infrastructure.Persistence;

public class TarotReadingRepository : MongoRepository<TarotReading>, ITarotReadingRepository
{
    public TarotReadingRepository(MongoContext context) : base(context)
    {
    }

    /// <summary>Історія гадань користувача: від нових до старих.</summary>
    public async Task<IReadOnlyList<TarotReading>> GetByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(r => r.UserId == userId)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <summary>Гадання лише якщо воно належить цьому користувачу.</summary>
    public async Task<TarotReading?> GetOwnedAsync(string id, string userId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(r => r.Id == id && r.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
}

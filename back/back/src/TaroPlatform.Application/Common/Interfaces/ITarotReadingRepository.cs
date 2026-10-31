using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Application.Common.Interfaces;

public interface ITarotReadingRepository : IRepository<TarotReading>
{
    Task<IReadOnlyList<TarotReading>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<TarotReading?> GetOwnedAsync(string id, string userId, CancellationToken cancellationToken = default);
}

using TaroPlatform.Domain.Matrix;

namespace TaroPlatform.Application.Common.Interfaces;

public interface ISavedMatrixRepository : IRepository<SavedMatrix>
{
    Task<IReadOnlyList<SavedMatrix>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<SavedMatrix?> GetOwnedAsync(string id, string userId, CancellationToken cancellationToken = default);
}

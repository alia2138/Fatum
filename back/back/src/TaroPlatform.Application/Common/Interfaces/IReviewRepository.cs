using TaroPlatform.Domain.Reviews;

namespace TaroPlatform.Application.Common.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    Task<Review?> GetByConsultationAsync(string consultationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetAllByConsultantAsync(string consultantId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Review> Items, long Total)> GetPageByConsultantAsync(string consultantId, int page, int pageSize, CancellationToken cancellationToken = default);
}

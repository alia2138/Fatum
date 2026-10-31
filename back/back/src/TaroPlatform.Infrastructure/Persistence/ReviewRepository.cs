using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Reviews;

namespace TaroPlatform.Infrastructure.Persistence;

public class ReviewRepository : MongoRepository<Review>, IReviewRepository
{
    public ReviewRepository(MongoContext context) : base(context)
    {
    }

    public async Task<Review?> GetByConsultationAsync(string consultationId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(r => r.ConsultationId == consultationId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Review>> GetAllByConsultantAsync(string consultantId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(r => r.ConsultantId == consultantId)
            .ToListAsync(cancellationToken);

    /// <summary>Сторінка відгуків таролога: від нових до старих.</summary>
    public async Task<(IReadOnlyList<Review> Items, long Total)> GetPageByConsultantAsync(string consultantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Review>.Filter.Eq(r => r.ConsultantId, consultantId);

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var found = await Collection
            .Find(filter)
            .SortByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyList<Review> items = found;
        return (items, total);
    }
}

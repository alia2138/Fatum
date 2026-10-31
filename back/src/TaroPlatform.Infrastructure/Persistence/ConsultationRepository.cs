using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Consultations;

namespace TaroPlatform.Infrastructure.Persistence;

public class ConsultationRepository : MongoRepository<Consultation>, IConsultationRepository
{
    public ConsultationRepository(MongoContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Consultation>> GetByParticipantAsync(string userId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(c => c.ClientId == userId || c.ConsultantId == userId)
            .SortByDescending(c => c.StartUtc)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasActiveAtAsync(string consultantId, DateTime startUtc, CancellationToken cancellationToken = default)
    {
        var f = Builders<Consultation>.Filter;
        var filter = f.And(
            f.Eq(c => c.ConsultantId, consultantId),
            f.Eq(c => c.StartUtc, startUtc),
            f.In(c => c.Status, new[] { ConsultationStatus.Pending, ConsultationStatus.Confirmed }));

        var count = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task<IReadOnlyList<Consultation>> GetDueRemindersAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var f = Builders<Consultation>.Filter;
        var filter = f.And(
            f.Eq(c => c.Status, ConsultationStatus.Confirmed),
            f.Gte(c => c.StartUtc, fromUtc),
            f.Lte(c => c.StartUtc, toUtc),
            f.Ne(c => c.ReminderSent, true));

        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
}

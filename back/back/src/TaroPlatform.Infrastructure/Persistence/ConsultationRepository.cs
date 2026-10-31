using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Consultations;

namespace TaroPlatform.Infrastructure.Persistence;

public class ConsultationRepository : MongoRepository<Consultation>, IConsultationRepository
{
    public ConsultationRepository(MongoContext context) : base(context)
    {
    }

    /// <summary>Усі заявки, де користувач є клієнтом або таро-консультантом.</summary>
    public async Task<IReadOnlyList<Consultation>> GetByParticipantAsync(string userId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(c => c.ClientId == userId || c.ConsultantId == userId)
            .SortByDescending(c => c.StartUtc)
            .ToListAsync(cancellationToken);

    /// <summary>Чи є в таролога активна заявка на цей час (щоб не записати двох клієнтів).</summary>
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
}

using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Chat;

namespace TaroPlatform.Infrastructure.Persistence;

public class ConversationRepository : MongoRepository<Conversation>, IConversationRepository
{
    public ConversationRepository(MongoContext context) : base(context)
    {
    }

    public async Task<Conversation?> GetByPairAsync(string clientId, string consultantId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(c => c.ClientId == clientId && c.ConsultantId == consultantId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Conversation>> GetByParticipantAsync(string userId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(c => c.ClientId == userId || c.ConsultantId == userId)
            .SortByDescending(c => c.LastMessageAt)
            .ToListAsync(cancellationToken);
}

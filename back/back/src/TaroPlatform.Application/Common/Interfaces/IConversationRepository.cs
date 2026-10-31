using TaroPlatform.Domain.Chat;

namespace TaroPlatform.Application.Common.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<Conversation?> GetByPairAsync(string clientId, string consultantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> GetByParticipantAsync(string userId, CancellationToken cancellationToken = default);
}

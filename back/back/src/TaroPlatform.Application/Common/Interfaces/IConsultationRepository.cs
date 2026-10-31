using TaroPlatform.Domain.Consultations;

namespace TaroPlatform.Application.Common.Interfaces;

public interface IConsultationRepository : IRepository<Consultation>
{
    Task<IReadOnlyList<Consultation>> GetByParticipantAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAtAsync(string consultantId, DateTime startUtc, CancellationToken cancellationToken = default);
}

using TaroPlatform.Application.Admin;

namespace TaroPlatform.Application.Common.Interfaces;

/// <summary>Залишок коштів або кредитів у провайдера AI. Ключ наружу не віддається.</summary>
public interface IAiBalanceProvider
{
    Task<AiBalanceDto> GetBalanceAsync(CancellationToken cancellationToken = default);
}

using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken = default);
}

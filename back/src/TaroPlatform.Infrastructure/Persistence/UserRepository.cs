using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Infrastructure.Persistence;

public class UserRepository : MongoRepository<User>, IUserRepository
{
    public UserRepository(MongoContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await Collection.Find(u => u.Email == email).FirstOrDefaultAsync(cancellationToken);

    public async Task<User?> GetByExternalLoginAsync(string provider, string providerUserId, CancellationToken cancellationToken = default) =>
        await Collection
            .Find(u => u.ExternalLogins.Any(e => e.Provider == provider && e.ProviderUserId == providerUserId))
            .FirstOrDefaultAsync(cancellationToken);
}

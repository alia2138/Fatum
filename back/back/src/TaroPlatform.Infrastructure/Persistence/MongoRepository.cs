using MongoDB.Driver;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Common;

namespace TaroPlatform.Infrastructure.Persistence;

public class MongoRepository<T> : IRepository<T> where T : Entity
{
    protected IMongoCollection<T> Collection { get; }

    public MongoRepository(MongoContext context)
    {
        var collectionName = typeof(T).Name.ToLowerInvariant() + "s";
        Collection = context.GetCollection<T>(collectionName);
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await Collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Collection.Find(_ => true).ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default) =>
        await Collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await Collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
}

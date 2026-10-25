using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TaroPlatform.Infrastructure.Persistence;

/// <summary>
/// Обгортка над MongoClient та IMongoDatabase.
/// Реєструється як Singleton: MongoClient призначений для повторного використання.
/// </summary>
public class MongoContext
{
    private readonly IMongoDatabase _database;

    public MongoContext(IOptions<MongoSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync(
                new BsonDocumentCommand<BsonDocument>(new BsonDocument("ping", 1)),
                cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

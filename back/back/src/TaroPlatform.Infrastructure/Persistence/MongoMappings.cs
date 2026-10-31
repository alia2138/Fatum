using MongoDB.Bson.Serialization;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Infrastructure.Persistence;

/// <summary>
/// Налаштування серіалізації для ієрархії User → Client / Consultant.
/// Mongo зберігає тип у полі _t, тож при читанні отримуємо правильний клас.
/// </summary>
public static class MongoMappings
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Sync)
        {
            if (_registered) return;

            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });
            BsonClassMap.RegisterClassMap<Client>(cm => cm.AutoMap());
            BsonClassMap.RegisterClassMap<Consultant>(cm => cm.AutoMap());

            _registered = true;
        }
    }
}

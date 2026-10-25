namespace TaroPlatform.Infrastructure.Persistence;

/// <summary>
/// Налаштування MongoDB. Прив'язується з appsettings через Options-патерн.
/// </summary>
public class MongoSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "TaroPlatform";
}

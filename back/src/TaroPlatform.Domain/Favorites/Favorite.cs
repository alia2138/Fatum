using TaroPlatform.Domain.Common;

namespace TaroPlatform.Domain.Favorites;

public enum FavoriteType
{
    Consultant = 0,
    Reading = 1
}

/// <summary>Обране: таролог або власне гадання користувача.</summary>
public class Favorite : Entity
{
    protected Favorite() { }

    public Favorite(string userId, FavoriteType type, string targetId)
    {
        UserId = userId;
        Type = type;
        TargetId = targetId;
    }

    public string UserId { get; private set; } = string.Empty;
    public FavoriteType Type { get; private set; }
    public string TargetId { get; private set; } = string.Empty;
}

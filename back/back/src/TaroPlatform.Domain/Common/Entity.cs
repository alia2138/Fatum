namespace TaroPlatform.Domain.Common;

/// <summary>
/// Базовий клас для всіх сутностей. Інкапсулює Id та дату створення.
/// Рівність сутностей визначається за типом і Id (патерн Entity).
/// </summary>
public abstract class Entity
{
    protected Entity()
    {
        Id = Guid.NewGuid().ToString("N");
        CreatedAt = DateTime.UtcNow;
    }

    public string Id { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

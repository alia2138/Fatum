using TaroPlatform.Domain.Enums;
using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Domain.Users;

/// <summary>
/// Клієнт таролога. Профіль: дата народження, часовий пояс, теми, що цікавлять.
/// </summary>
public class Client : User
{
    protected Client() { }

    public Client(string email, string passwordHash, DateTime birthDate)
        : base(email, passwordHash, UserRole.Client)
    {
        BirthDate = birthDate.Date;
    }

    public DateTime BirthDate { get; private set; }
    public string? Timezone { get; private set; }

    /// <summary>Теми, які цікавлять клієнта (ті самі, що в гаданнях).</summary>
    public List<ReadingTopic> Interests { get; private set; } = new();

    public void SetBirthDate(DateTime date) => BirthDate = date.Date;

    public void SetTimezone(string? timezone) =>
        Timezone = string.IsNullOrWhiteSpace(timezone) ? null : timezone;

    public void SetInterests(IEnumerable<ReadingTopic> topics) => Interests = topics.Distinct().ToList();

    public override IReadOnlyList<string> GetMissingProfileFields()
    {
        var missing = base.GetMissingProfileFields().ToList();
        if (Interests.Count == 0)
            missing.Add("Теми, що цікавлять");
        return missing;
    }
}

using TaroPlatform.Domain.Enums;

namespace TaroPlatform.Domain.Users;

/// <summary>
/// Клієнт таролога. Окрім базових полів має дату народження.
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
}

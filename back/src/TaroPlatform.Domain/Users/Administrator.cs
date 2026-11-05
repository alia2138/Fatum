using TaroPlatform.Domain.Enums;

namespace TaroPlatform.Domain.Users;

/// <summary>Адміністратор. Створюється лише з конфігурації, самостійно не реєструється.</summary>
public class Administrator : User
{
    protected Administrator() { }

    public Administrator(string email, string passwordHash)
        : base(email, passwordHash, UserRole.Admin)
    {
    }
}

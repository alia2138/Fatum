using TaroPlatform.Domain.Enums;

namespace TaroPlatform.Domain.Users;

/// <summary>
/// Таролог (консультант). Поля, специфічні для профілю, додамо на наступному кроці.
/// </summary>
public class Consultant : User
{
    protected Consultant() { }

    public Consultant(string email, string passwordHash)
        : base(email, passwordHash, UserRole.Consultant)
    {
    }
}

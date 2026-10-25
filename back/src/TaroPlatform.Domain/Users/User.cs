using TaroPlatform.Domain.Common;
using TaroPlatform.Domain.Enums;

namespace TaroPlatform.Domain.Users;

/// <summary>
/// Базовий клас користувача. Email і пароль можуть бути порожніми,
/// якщо користувач зайшов лише через соцмережу (наприклад, Telegram не передає email).
/// </summary>
public abstract class User : Entity
{
    protected User() { }

    protected User(string email, string passwordHash, UserRole role)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    public List<ExternalLogin> ExternalLogins { get; private set; } = new();

    public bool HasExternalLogin(string provider, string providerUserId) =>
        ExternalLogins.Any(e => e.Provider == provider && e.ProviderUserId == providerUserId);

    public void AddExternalLogin(string provider, string providerUserId)
    {
        if (!HasExternalLogin(provider, providerUserId))
            ExternalLogins.Add(new ExternalLogin(provider, providerUserId));
    }
}

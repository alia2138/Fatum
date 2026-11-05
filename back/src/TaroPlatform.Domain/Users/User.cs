using TaroPlatform.Domain.Common;
using TaroPlatform.Domain.Enums;

namespace TaroPlatform.Domain.Users;

/// <summary>
/// Базовий клас користувача: вхід і спільні поля профілю.
/// Кожен тип користувача сам визначає, які поля профілю обов'язкові (поліморфізм).
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

    public string DisplayName { get; private set; } = string.Empty;
    public string? PhotoUrl { get; private set; }
    public string? Bio { get; private set; }

    /// <summary>Заблокований користувач не може увійти.</summary>
    public bool IsBlocked { get; private set; }

    public void Block() => IsBlocked = true;

    public void Unblock() => IsBlocked = false;

    public bool HasExternalLogin(string provider, string providerUserId) =>
        ExternalLogins.Any(e => e.Provider == provider && e.ProviderUserId == providerUserId);

    public void AddExternalLogin(string provider, string providerUserId)
    {
        if (!HasExternalLogin(provider, providerUserId))
            ExternalLogins.Add(new ExternalLogin(provider, providerUserId));
    }

    public void SetDisplayName(string name) => DisplayName = name;

    public void SetBio(string? bio) => Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;

    public void SetPhotoUrl(string? url) => PhotoUrl = url;

    /// <summary>Список незаповнених обов'язкових полів. Підкласи додають свої.</summary>
    public virtual IReadOnlyList<string> GetMissingProfileFields()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(DisplayName))
            missing.Add("Ім'я");
        return missing;
    }

    public bool IsProfileComplete() => GetMissingProfileFields().Count == 0;
}

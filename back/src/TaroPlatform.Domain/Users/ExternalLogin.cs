namespace TaroPlatform.Domain.Users;

/// <summary>
/// Прив'язка акаунта до зовнішнього провайдера (google, github, telegram).
/// Один користувач може мати кілька способів входу.
/// </summary>
public class ExternalLogin
{
    protected ExternalLogin() { }

    public ExternalLogin(string provider, string providerUserId)
    {
        Provider = provider;
        ProviderUserId = providerUserId;
    }

    public string Provider { get; private set; } = string.Empty;
    public string ProviderUserId { get; private set; } = string.Empty;
}

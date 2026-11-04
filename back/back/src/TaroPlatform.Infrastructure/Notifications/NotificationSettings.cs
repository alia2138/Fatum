namespace TaroPlatform.Infrastructure.Notifications;

/// <summary>
/// Налаштування зовнішніх сповіщень. Паролі SMTP — у appsettings.Local.json.
/// Якщо SmtpHost порожній, email не надсилається. Telegram використовує BotToken з секції Telegram.
/// </summary>
public class NotificationSettings
{
    public const string SectionName = "Notifications";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SmtpFrom { get; set; } = string.Empty;
    public bool SmtpEnableSsl { get; set; } = true;

    /// <summary>Адреса фронту, щоб у повідомленнях були повні посилання.</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:63342/front/dist/html";
}

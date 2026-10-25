namespace TaroPlatform.Infrastructure.Security;

public class TelegramSettings
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;
}

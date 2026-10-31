namespace TaroPlatform.Infrastructure.AI;

/// <summary>
/// Налаштування мовної моделі. Працює з будь-яким OpenAI-сумісним сервісом:
/// OpenAI, OpenRouter, Groq тощо. Ключ зберігається в appsettings.Local.json.
/// </summary>
public class AiSettings
{
    public const string SectionName = "Ai";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxTokens { get; set; } = 800;

    /// <summary>AI вмикається лише коли увімкнено і задано ключ.</summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}

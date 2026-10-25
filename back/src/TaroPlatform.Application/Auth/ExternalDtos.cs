using System.Text.Json.Serialization;
using TaroPlatform.Domain.Enums;

namespace TaroPlatform.Application.Auth;

/// <summary>
/// Дані, нормалізовані від будь-якого зовнішнього провайдера.
/// </summary>
public record ExternalIdentity(
    string Provider,
    string ProviderUserId,
    string? Email,
    UserRole Role,
    DateTime? BirthDate);

/// <summary>
/// Дані, які віджет Telegram віддає на сторінку, плюс роль і дата народження від фронтенду.
/// Назви полів збігаються з тим, що надсилає Telegram.
/// </summary>
public record TelegramLoginRequest(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("first_name")] string? FirstName,
    [property: JsonPropertyName("last_name")] string? LastName,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("photo_url")] string? PhotoUrl,
    [property: JsonPropertyName("auth_date")] long AuthDate,
    [property: JsonPropertyName("hash")] string Hash,
    UserRole Role,
    DateTime? BirthDate);

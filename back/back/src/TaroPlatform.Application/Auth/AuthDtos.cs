using TaroPlatform.Domain.Enums;

namespace TaroPlatform.Application.Auth;

/// <summary>
/// Дані реєстрації. Для клієнта потрібна BirthDate, для таролога вона не використовується.
/// </summary>
public record RegisterRequest(
    UserRole Role,
    string Email,
    string Password,
    string ConfirmPassword,
    DateTime? BirthDate);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, string UserId, string Email, string Role);

public record UserProfileResponse(string Id, string Email, string Role, DateTime? BirthDate);

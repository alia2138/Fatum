using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Admin;

/// <summary>
/// Створює адміністратора з конфігурації (Admin:Email, Admin:Password), якщо такого email ще немає.
/// Існуючий користувач із цим email не змінюється.
/// </summary>
public class AdminBootstrap
{
    private const int MinPasswordLength = 6;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public AdminBootstrap(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task EnsureAsync(string? email, string? password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;
        if (password.Length < MinPasswordLength)
            return;

        var normalized = email.Trim().ToLowerInvariant();
        if (await _users.GetByEmailAsync(normalized, cancellationToken) is not null)
            return;

        await _users.AddAsync(new Administrator(normalized, _hasher.Hash(password)), cancellationToken);
    }
}

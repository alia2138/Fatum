using System.Text.RegularExpressions;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> ExternalLoginAsync(ExternalIdentity identity, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> TelegramLoginAsync(TelegramLoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserProfileResponse>> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private const int MinPasswordLength = 6;
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ITelegramVerifier _telegram;

    public AuthService(
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtTokenGenerator jwt,
        ITelegramVerifier telegram)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _telegram = telegram;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var validation = ValidateCredentials(email, request.Password, request.ConfirmPassword);
        if (validation.IsFailure)
            return Result.Failure<AuthResponse>(validation.Error!);

        if (await _users.GetByEmailAsync(email, cancellationToken) is not null)
            return Result.Failure<AuthResponse>("Користувач з таким email вже існує.");

        var passwordHash = _hasher.Hash(request.Password);
        var created = UserFactory.Create(request with { Email = email }, passwordHash);
        if (created.IsFailure)
            return Result.Failure<AuthResponse>(created.Error!);

        await _users.AddAsync(created.Value, cancellationToken);

        return Result.Success(ToAuthResponse(created.Value));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        if (user is not null && user.IsBlocked)
            return Result.Failure<AuthResponse>("Акаунт заблоковано. Зверніться до адміністратора.");

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>("Невірний email або пароль.");

        return Result.Success(ToAuthResponse(user));
    }

    /// <summary>
    /// Вхід або реєстрація через соцмережу. Порядок:
    /// 1) акаунт уже прив'язаний до цього провайдера;
    /// 2) є користувач з таким email, прив'язуємо провайдер до нього;
    /// 3) створюємо нового користувача з обраною роллю.
    /// </summary>
    public async Task<Result<AuthResponse>> ExternalLoginAsync(ExternalIdentity identity, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByExternalLoginAsync(identity.Provider, identity.ProviderUserId, cancellationToken);

        if (user is null && !string.IsNullOrWhiteSpace(identity.Email))
        {
            user = await _users.GetByEmailAsync(NormalizeEmail(identity.Email), cancellationToken);
            if (user is not null)
            {
                user.AddExternalLogin(identity.Provider, identity.ProviderUserId);
                await _users.UpdateAsync(user, cancellationToken);
            }
        }

        if (user is not null && user.IsBlocked)
            return Result.Failure<AuthResponse>("Акаунт заблоковано. Зверніться до адміністратора.");

        if (user is null)
        {
            var request = new RegisterRequest(
                identity.Role,
                NormalizeEmail(identity.Email),
                string.Empty,
                string.Empty,
                identity.BirthDate);

            var created = UserFactory.Create(request, passwordHash: string.Empty);
            if (created.IsFailure)
                return Result.Failure<AuthResponse>(created.Error!);

            user = created.Value;
            user.AddExternalLogin(identity.Provider, identity.ProviderUserId);
            await _users.AddAsync(user, cancellationToken);
        }

        return Result.Success(ToAuthResponse(user));
    }

    public async Task<Result<AuthResponse>> TelegramLoginAsync(TelegramLoginRequest request, CancellationToken cancellationToken = default)
    {
        // Telegram підписує лише поля, які реально прийшли, тому збираємо їх у словник
        var fields = new Dictionary<string, string>
        {
            ["id"] = request.Id.ToString(),
            ["auth_date"] = request.AuthDate.ToString()
        };

        if (request.FirstName is not null) fields["first_name"] = request.FirstName;
        if (request.LastName is not null) fields["last_name"] = request.LastName;
        if (request.Username is not null) fields["username"] = request.Username;
        if (request.PhotoUrl is not null) fields["photo_url"] = request.PhotoUrl;

        if (!_telegram.Verify(fields, request.Hash, request.AuthDate))
            return Result.Failure<AuthResponse>("Не вдалося підтвердити дані Telegram.");

        var identity = new ExternalIdentity(
            "telegram",
            request.Id.ToString(),
            null,
            request.Role,
            request.BirthDate);

        return await ExternalLoginAsync(identity, cancellationToken);
    }

    public async Task<Result<UserProfileResponse>> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<UserProfileResponse>("Користувача не знайдено.");

        // Поліморфізм: дата народження є лише у клієнта
        DateTime? birthDate = user is Client client ? client.BirthDate : null;

        return Result.Success(new UserProfileResponse(user.Id, user.Email, user.Role.ToString(), birthDate));
    }

    private static string NormalizeEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static Result ValidateCredentials(string email, string password, string confirmPassword)
    {
        if (!EmailPattern.IsMatch(email))
            return Result.Failure("Некоректний email.");

        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            return Result.Failure($"Пароль має містити щонайменше {MinPasswordLength} символів.");

        if (password != confirmPassword)
            return Result.Failure("Паролі не співпадають.");

        return Result.Success();
    }

    private AuthResponse ToAuthResponse(User user)
    {
        var token = _jwt.Generate(user);
        return new AuthResponse(token, user.Id, user.Email, user.Role.ToString());
    }
}

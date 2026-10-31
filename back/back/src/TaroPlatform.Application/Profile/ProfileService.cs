using System.Text.RegularExpressions;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Profile;

public interface IProfileService
{
    Task<Result<ProfileDto>> GetMeAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<ProfileDto>> UpdateClientAsync(string userId, ClientProfileUpdate request, CancellationToken cancellationToken = default);
    Task<Result<ProfileDto>> UpdateConsultantAsync(string userId, ConsultantProfileUpdate request, CancellationToken cancellationToken = default);
    Task<Result<ProfileDto>> UploadPhotoAsync(string userId, Stream content, string contentType, long length, CancellationToken cancellationToken = default);
    Task<Result<ProfileDto>> DeletePhotoAsync(string userId, CancellationToken cancellationToken = default);
}

public class ProfileService : IProfileService
{
    private const int MaxPhotoBytes = 2 * 1024 * 1024;
    private const int MaxTopics = 10;
    private const int MaxLanguages = 5;
    private const int MaxExperienceYears = 80;
    private const int MaxPriceUah = 100_000;

    private static readonly IReadOnlyDictionary<string, string> PhotoExtensions =
        new Dictionary<string, string>
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    private static readonly Regex TelegramPattern = new(@"^[A-Za-z0-9_]{5,32}$");

    private readonly IUserRepository _users;
    private readonly IPhotoStorage _photos;

    public ProfileService(IUserRepository users, IPhotoStorage photos)
    {
        _users = users;
        _photos = photos;
    }

    public async Task<Result<ProfileDto>> GetMeAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<ProfileDto>("Користувача не знайдено.");

        return Result.Success(ToDto(user));
    }

    public async Task<Result<ProfileDto>> UpdateClientAsync(string userId, ClientProfileUpdate request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<ProfileDto>("Користувача не знайдено.");
        if (user is not Client client)
            return Result.Failure<ProfileDto>("Ця дія доступна лише клієнтам.");

        var error = ValidateBasics(request.DisplayName, request.Bio)
                    ?? ValidateBirthDate(request.BirthDate)
                    ?? ValidateTimezone(request.Timezone)
                    ?? ValidateTopics(request.Interests);
        if (error is not null)
            return Result.Failure<ProfileDto>(error);

        ApplyBasics(client, request.DisplayName, request.Bio);
        if (request.BirthDate is { } birthDate)
            client.SetBirthDate(birthDate);
        if (request.Timezone is not null)
            client.SetTimezone(request.Timezone.Trim());
        if (request.Interests is not null)
            client.SetInterests(request.Interests);

        await _users.UpdateAsync(client, cancellationToken);
        return Result.Success(ToDto(client));
    }

    public async Task<Result<ProfileDto>> UpdateConsultantAsync(string userId, ConsultantProfileUpdate request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<ProfileDto>("Користувача не знайдено.");
        if (user is not Consultant consultant)
            return Result.Failure<ProfileDto>("Ця дія доступна лише таро-консультантам.");

        var error = ValidateBasics(request.DisplayName, request.Bio)
                    ?? ValidateConsultant(request);
        if (error is not null)
            return Result.Failure<ProfileDto>(error);

        ApplyBasics(consultant, request.DisplayName, request.Bio);
        if (request.Specializations is not null)
            consultant.SetSpecializations(request.Specializations);
        if (request.ExperienceYears is { } years)
            consultant.SetExperienceYears(years);
        if (request.Languages is not null)
            consultant.SetLanguages(request.Languages.Select(l => l.Trim()).Where(l => l.Length > 0));
        if (request.PriceUah is { } price)
            consultant.SetPriceUah(price);
        if (request.SessionMinutes is { } minutes)
            consultant.SetSessionMinutes(minutes);
        if (request.ContactTelegram is not null)
            consultant.SetContactTelegram(NormalizeTelegram(request.ContactTelegram));

        // Прийом клієнтів відкривається лише з повним профілем
        if (request.IsAcceptingClients is { } accepting)
        {
            if (accepting && !consultant.IsProfileComplete())
            {
                var missing = string.Join(", ", consultant.GetMissingProfileFields());
                return Result.Failure<ProfileDto>($"Щоб приймати клієнтів, заповніть профіль: {missing}.");
            }

            consultant.SetAcceptingClients(accepting);
        }

        // Якщо профіль став неповним, прийом клієнтів вимикається автоматично
        if (!consultant.IsProfileComplete())
            consultant.SetAcceptingClients(false);

        await _users.UpdateAsync(consultant, cancellationToken);
        return Result.Success(ToDto(consultant));
    }

    public async Task<Result<ProfileDto>> UploadPhotoAsync(string userId, Stream content, string contentType, long length, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<ProfileDto>("Користувача не знайдено.");

        if (!PhotoExtensions.TryGetValue((contentType ?? string.Empty).ToLowerInvariant(), out var extension))
            return Result.Failure<ProfileDto>("Дозволені формати фото: JPG, PNG, WEBP.");

        if (length <= 0 || length > MaxPhotoBytes)
            return Result.Failure<ProfileDto>("Фото має бути не більшим за 2 МБ.");

        var previous = user.PhotoUrl;
        var url = await _photos.SaveAsync(userId, content, extension, cancellationToken);

        user.SetPhotoUrl(url);
        await _users.UpdateAsync(user, cancellationToken);

        if (!string.IsNullOrEmpty(previous))
            await _photos.DeleteAsync(previous, cancellationToken);

        return Result.Success(ToDto(user));
    }

    public async Task<Result<ProfileDto>> DeletePhotoAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<ProfileDto>("Користувача не знайдено.");

        if (string.IsNullOrEmpty(user.PhotoUrl))
            return Result.Success(ToDto(user));

        await _photos.DeleteAsync(user.PhotoUrl, cancellationToken);
        user.SetPhotoUrl(null);
        if (user is Consultant consultant && !consultant.IsProfileComplete())
            consultant.SetAcceptingClients(false);
        await _users.UpdateAsync(user, cancellationToken);

        return Result.Success(ToDto(user));
    }

    // ---------- Валідація ----------

    private static string? ValidateBasics(string? displayName, string? bio)
    {
        if (displayName is not null)
        {
            var name = displayName.Trim();
            if (name.Length < 2 || name.Length > 50)
                return "Ім'я має містити від 2 до 50 символів.";
        }

        if (bio is not null && bio.Trim().Length > 1000)
            return "Опис не може бути довшим за 1000 символів.";

        return null;
    }

    private static string? ValidateBirthDate(DateTime? birthDate)
    {
        if (birthDate is { } date && (date.Date > DateTime.UtcNow.Date || date.Year < 1900))
            return "Некоректна дата народження.";

        return null;
    }

    private static string? ValidateTimezone(string? timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            return null;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezone.Trim());
            return null;
        }
        catch (TimeZoneNotFoundException)
        {
            return "Невірний часовий пояс, наприклад Europe/Kyiv.";
        }
        catch (InvalidTimeZoneException)
        {
            return "Невірний часовий пояс, наприклад Europe/Kyiv.";
        }
    }

    private static string? ValidateTopics(List<Domain.Tarot.ReadingTopic>? topics)
    {
        if (topics is not null && topics.Distinct().Count() > MaxTopics)
            return $"Можна обрати не більше {MaxTopics} тем.";

        return null;
    }

    private static string? ValidateConsultant(ConsultantProfileUpdate request)
    {
        if (request.Specializations is not null && request.Specializations.Distinct().Count() > MaxTopics)
            return $"Можна обрати не більше {MaxTopics} спеціалізацій.";

        if (request.ExperienceYears is { } years && (years < 0 || years > MaxExperienceYears))
            return $"Стаж має бути від 0 до {MaxExperienceYears} років.";

        if (request.Languages is not null)
        {
            var languages = request.Languages.Select(l => l.Trim()).Where(l => l.Length > 0).Distinct().ToList();
            if (languages.Count > MaxLanguages)
                return $"Можна вказати не більше {MaxLanguages} мов.";
            if (languages.Any(l => l.Length > 30))
                return "Назва мови має бути не довшою за 30 символів.";
        }

        if (request.PriceUah is { } price && (price < 0 || price > MaxPriceUah))
            return $"Ціна має бути від 0 до {MaxPriceUah} грн.";

        if (request.SessionMinutes is { } minutes && (minutes < 15 || minutes > 180 || minutes % 15 != 0))
            return "Тривалість має бути від 15 до 180 хвилин, кратною 15.";

        if (request.ContactTelegram is not null)
        {
            var telegram = NormalizeTelegram(request.ContactTelegram);
            if (telegram is not null && !TelegramPattern.IsMatch(telegram))
                return "Некоректний Telegram-нік (5–32 символи: літери, цифри, підкреслення).";
        }

        return null;
    }

    // ---------- Допоміжні ----------

    private static void ApplyBasics(User user, string? displayName, string? bio)
    {
        if (displayName is not null)
            user.SetDisplayName(displayName.Trim());
        if (bio is not null)
            user.SetBio(bio.Trim());
    }

    private static string? NormalizeTelegram(string input)
    {
        var value = input.Trim().TrimStart('@');
        return value.Length == 0 ? null : value;
    }

    private static ProfileDto ToDto(User user)
    {
        var missing = user.GetMissingProfileFields();
        ClientProfileDto? clientProfile = null;
        ConsultantProfileDto? consultantProfile = null;

        if (user is Client client)
        {
            clientProfile = new ClientProfileDto(
                client.BirthDate,
                client.Timezone,
                client.Interests.Select(t => t.ToString()).ToList());
        }
        else if (user is Consultant consultant)
        {
            consultantProfile = new ConsultantProfileDto(
                consultant.Specializations.Select(t => t.ToString()).ToList(),
                consultant.ExperienceYears,
                consultant.Languages,
                consultant.PriceUah,
                consultant.SessionMinutes,
                consultant.IsAcceptingClients,
                consultant.ContactTelegram);
        }

        return new ProfileDto(
            user.Id,
            user.Role.ToString(),
            user.Email,
            user.DisplayName,
            user.PhotoUrl,
            user.Bio,
            missing.Count == 0,
            missing,
            clientProfile,
            consultantProfile);
    }
}

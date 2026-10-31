using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Application.Profile;

/// <summary>
/// Повний профіль. Client заповнений для клієнтів, Consultant — для таro-консультантів.
/// IsComplete: чи заповнені всі обов'язкові поля, MissingFields — що ще треба заповнити.
/// </summary>
public record ProfileDto(
    string Id,
    string Role,
    string Email,
    string DisplayName,
    string? PhotoUrl,
    string? Bio,
    bool IsComplete,
    IReadOnlyList<string> MissingFields,
    ClientProfileDto? Client,
    ConsultantProfileDto? Consultant);

public record ClientProfileDto(
    DateTime BirthDate,
    string? Timezone,
    IReadOnlyList<string> Interests);

public record ConsultantProfileDto(
    IReadOnlyList<string> Specializations,
    int ExperienceYears,
    IReadOnlyList<string> Languages,
    int PriceUah,
    int SessionMinutes,
    bool IsAcceptingClients,
    string? ContactTelegram);

/// <summary>Часткове оновлення: null означає «не змінювати».</summary>
public record ClientProfileUpdate(
    string? DisplayName,
    string? Bio,
    DateTime? BirthDate,
    string? Timezone,
    List<ReadingTopic>? Interests);

/// <summary>Часткове оновлення: null означає «не змінювати».</summary>
public record ConsultantProfileUpdate(
    string? DisplayName,
    string? Bio,
    List<ReadingTopic>? Specializations,
    int? ExperienceYears,
    List<string>? Languages,
    int? PriceUah,
    int? SessionMinutes,
    bool? IsAcceptingClients,
    string? ContactTelegram);

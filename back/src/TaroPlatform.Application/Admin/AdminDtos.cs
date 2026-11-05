namespace TaroPlatform.Application.Admin;

public record AdminUserDto(
    string Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsBlocked,
    DateTime CreatedAt);

public record AdminUserDetailDto(
    string Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsBlocked,
    DateTime CreatedAt,
    bool IsProfileComplete,
    int ReadingsCount,
    int ConsultationsCount,
    int MatricesCount);

/// <summary>Баланс AI. Supported=false означає, що провайдер не віддає баланс через API.</summary>
public record AiBalanceDto(
    string Provider,
    bool Supported,
    double? Usage,
    double? Limit,
    double? Remaining,
    string Note);

public record AdminStatsDto(
    int TotalUsers,
    IReadOnlyDictionary<string, int> UsersByRole,
    int BlockedUsers,
    int TotalConsultations,
    IReadOnlyDictionary<string, int> ConsultationsByStatus,
    int TotalReadings,
    int TotalMatrices,
    int TotalReviews,
    int AiReadingAnswers,
    int AiMatrixAnswers,
    IReadOnlyList<AdminUserDto> RecentUsers,
    AiBalanceDto Ai);

using TaroPlatform.Application.Catalog;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Enums;
using TaroPlatform.Domain.Matrix;
using TaroPlatform.Domain.Tarot;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Admin;

public interface IAdminService
{
    Task<Result<PagedResult<AdminUserDto>>> ListUsersAsync(string? role, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDetailDto>> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result> SetBlockedAsync(string adminId, string userId, bool blocked, CancellationToken cancellationToken = default);
    Task<Result> DeleteReviewAsync(string reviewId, CancellationToken cancellationToken = default);
    Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}

public class AdminService : IAdminService
{
    private const int MaxPageSize = 100;
    private const int RecentCount = 10;

    private readonly IUserRepository _users;
    private readonly ITarotReadingRepository _readings;
    private readonly IConsultationRepository _consultations;
    private readonly ISavedMatrixRepository _matrices;
    private readonly IReviewRepository _reviews;
    private readonly IAiBalanceProvider _balance;

    public AdminService(
        IUserRepository users,
        ITarotReadingRepository readings,
        IConsultationRepository consultations,
        ISavedMatrixRepository matrices,
        IReviewRepository reviews,
        IAiBalanceProvider balance)
    {
        _users = users;
        _readings = readings;
        _consultations = consultations;
        _matrices = matrices;
        _reviews = reviews;
        _balance = balance;
    }

    public async Task<Result<PagedResult<AdminUserDto>>> ListUsersAsync(string? role, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return Result.Failure<PagedResult<AdminUserDto>>("Номер сторінки має бути 1 або більше.");
        if (pageSize < 1 || pageSize > MaxPageSize)
            return Result.Failure<PagedResult<AdminUserDto>>($"Розмір сторінки: від 1 до {MaxPageSize}.");

        UserRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
                return Result.Failure<PagedResult<AdminUserDto>>("Роль: Client, Consultant або Admin.");
            roleFilter = parsed;
        }

        var all = await _users.GetAllAsync(cancellationToken);
        var query = all.AsEnumerable();

        if (roleFilter is not null)
            query = query.Where(u => u.Role == roleFilter);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                u.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderByDescending(u => u.CreatedAt).ToList();
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToSummary)
            .ToList();

        return Result.Success(new PagedResult<AdminUserDto>(items, ordered.Count, page, pageSize));
    }

    public async Task<Result<AdminUserDetailDto>> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<AdminUserDetailDto>("Користувача не знайдено.");

        var readings = await _readings.GetByUserAsync(userId, cancellationToken);
        var consultations = await _consultations.GetByParticipantAsync(userId, cancellationToken);
        var matrices = await _matrices.GetByUserAsync(userId, cancellationToken);

        return Result.Success(new AdminUserDetailDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString(),
            user.IsBlocked,
            user.CreatedAt,
            user.IsProfileComplete(),
            readings.Count,
            consultations.Count,
            matrices.Count));
    }

    public async Task<Result> SetBlockedAsync(string adminId, string userId, bool blocked, CancellationToken cancellationToken = default)
    {
        if (adminId == userId)
            return Result.Failure("Не можна заблокувати або розблокувати самого себе.");

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure("Користувача не знайдено.");
        if (user is Administrator)
            return Result.Failure("Адміністратора не можна заблокувати.");

        if (blocked)
            user.Block();
        else
            user.Unblock();

        await _users.UpdateAsync(user, cancellationToken);
        return Result.Success();
    }

    /// <summary>Видаляє відгук і перераховує рейтинг таролога.</summary>
    public async Task<Result> DeleteReviewAsync(string reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _reviews.GetByIdAsync(reviewId, cancellationToken);
        if (review is null)
            return Result.Failure("Відгук не знайдено.");

        var consultantId = review.ConsultantId;
        await _reviews.DeleteAsync(review.Id, cancellationToken);

        var left = await _reviews.GetAllByConsultantAsync(consultantId, cancellationToken);
        var average = left.Count == 0 ? 0 : Math.Round(left.Average(r => (double)r.Rating), 2);

        if (await _users.GetByIdAsync(consultantId, cancellationToken) is Consultant consultant)
        {
            consultant.SetRating(average, left.Count);
            await _users.UpdateAsync(consultant, cancellationToken);
        }

        return Result.Success();
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.GetAllAsync(cancellationToken);
        var consultations = await _consultations.GetAllAsync(cancellationToken);
        var readings = await _readings.GetAllAsync(cancellationToken);
        var matrices = await _matrices.GetAllAsync(cancellationToken);
        var reviews = await _reviews.GetAllAsync(cancellationToken);

        var usersByRole = Enum.GetValues<UserRole>()
            .ToDictionary(r => r.ToString(), r => users.Count(u => u.Role == r));

        var consultationsByStatus = Enum.GetValues<TaroPlatform.Domain.Consultations.ConsultationStatus>()
            .ToDictionary(s => s.ToString(), s => consultations.Count(c => c.Status == s));

        var aiReadingAnswers = readings
            .Sum(r => r.Messages.Count(m => m.Role == MessageRole.Assistant && m.Provider == "ai"));

        var aiMatrixAnswers = matrices
            .Sum(m => m.Messages.Count(x => x.Role == MatrixMessageRole.Assistant && x.Provider == "ai"));

        var recent = users
            .OrderByDescending(u => u.CreatedAt)
            .Take(RecentCount)
            .Select(ToSummary)
            .ToList();

        var balance = await _balance.GetBalanceAsync(cancellationToken);

        return new AdminStatsDto(
            users.Count,
            usersByRole,
            users.Count(u => u.IsBlocked),
            consultations.Count,
            consultationsByStatus,
            readings.Count,
            matrices.Count,
            reviews.Count,
            aiReadingAnswers,
            aiMatrixAnswers,
            recent,
            balance);
    }

    private static AdminUserDto ToSummary(TaroPlatform.Domain.Users.User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role.ToString(), user.IsBlocked, user.CreatedAt);
}

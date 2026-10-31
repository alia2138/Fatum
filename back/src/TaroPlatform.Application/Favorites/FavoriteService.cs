using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Favorites;
using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Favorites;

public record AddFavoriteRequest(FavoriteType Type, string TargetId);

public record FavoriteDto(string Id, string Type, string TargetId, string Title, DateTime CreatedAt);

public interface IFavoriteService
{
    Task<Result<FavoriteDto>> AddAsync(string userId, AddFavoriteRequest request, CancellationToken cancellationToken = default);
    Task<Result> RemoveAsync(string userId, string favoriteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FavoriteDto>> GetMineAsync(string userId, FavoriteType? type = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Обране: тарологи з каталогу і власні гадання. Чуже гадання в обране не додати.
/// </summary>
public class FavoriteService : IFavoriteService
{
    private const int MaxTitleLength = 80;

    private readonly IFavoriteRepository _favorites;
    private readonly IUserRepository _users;
    private readonly ITarotReadingRepository _readings;

    public FavoriteService(IFavoriteRepository favorites, IUserRepository users, ITarotReadingRepository readings)
    {
        _favorites = favorites;
        _users = users;
        _readings = readings;
    }

    public async Task<Result<FavoriteDto>> AddAsync(string userId, AddFavoriteRequest request, CancellationToken cancellationToken = default)
    {
        if (await _favorites.FindAsync(userId, request.Type, request.TargetId, cancellationToken) is not null)
            return Result.Failure<FavoriteDto>("Це вже є в обраному.");

        string title;
        if (request.Type == FavoriteType.Consultant)
        {
            if (await _users.GetByIdAsync(request.TargetId, cancellationToken) is not Consultant consultant)
                return Result.Failure<FavoriteDto>("Таролога не знайдено.");
            title = consultant.DisplayName;
        }
        else
        {
            var reading = await _readings.GetOwnedAsync(request.TargetId, userId, cancellationToken);
            if (reading is null)
                return Result.Failure<FavoriteDto>("Гадання не знайдено.");
            title = Shorten(reading.Question);
        }

        var favorite = new Favorite(userId, request.Type, request.TargetId);
        await _favorites.AddAsync(favorite, cancellationToken);

        return Result.Success(new FavoriteDto(favorite.Id, request.Type.ToString(), request.TargetId, title, favorite.CreatedAt));
    }

    public async Task<Result> RemoveAsync(string userId, string favoriteId, CancellationToken cancellationToken = default)
    {
        var favorite = await _favorites.GetByIdAsync(favoriteId, cancellationToken);
        if (favorite is null || favorite.UserId != userId)
            return Result.Failure("Запис в обраному не знайдено.");

        await _favorites.DeleteAsync(favorite.Id, cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<FavoriteDto>> GetMineAsync(string userId, FavoriteType? type = null, CancellationToken cancellationToken = default)
    {
        var items = await _favorites.GetByUserAsync(userId, cancellationToken);
        var result = new List<FavoriteDto>();

        foreach (var favorite in items.Where(f => type is null || f.Type == type))
        {
            var title = await TitleAsync(favorite, cancellationToken);
            if (title is null)
                continue;

            result.Add(new FavoriteDto(favorite.Id, favorite.Type.ToString(), favorite.TargetId, title, favorite.CreatedAt));
        }

        return result;
    }

    private async Task<string?> TitleAsync(Favorite favorite, CancellationToken cancellationToken)
    {
        if (favorite.Type == FavoriteType.Consultant)
        {
            var user = await _users.GetByIdAsync(favorite.TargetId, cancellationToken);
            return user?.DisplayName;
        }

        var reading = await _readings.GetByIdAsync(favorite.TargetId, cancellationToken);
        return reading is null ? null : Shorten(reading.Question);
    }

    private static string Shorten(string text) =>
        text.Length <= MaxTitleLength ? text : text[..MaxTitleLength] + "…";
}

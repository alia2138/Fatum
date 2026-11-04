using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Favorites;
using TaroPlatform.Domain.Favorites;

namespace TaroPlatform.Api.Controllers;

/// <summary>Обране: таологи і власні гадання.</summary>
[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favorites;

    public FavoritesController(IFavoriteService favorites)
    {
        _favorites = favorites;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    /// <summary>Мій список. Фільтр type: Consultant або Reading.</summary>
    [HttpGet]
    public async Task<IActionResult> Mine([FromQuery] FavoriteType? type, CancellationToken cancellationToken)
    {
        var list = await _favorites.GetMineAsync(UserId, type, cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddFavoriteRequest request, CancellationToken cancellationToken)
    {
        var result = await _favorites.AddAsync(UserId, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(string id, CancellationToken cancellationToken)
    {
        var result = await _favorites.RemoveAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return NoContent();
    }
}

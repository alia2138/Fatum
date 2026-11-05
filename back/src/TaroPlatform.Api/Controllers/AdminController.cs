using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Admin;

namespace TaroPlatform.Api.Controllers;

/// <summary>Адмін-панель: користувачі, блокування, модерація відгуків, статистика і баланс AI.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin)
    {
        _admin = admin;
    }

    private string AdminId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора адміністратора.");

    [HttpGet("users")]
    public async Task<IActionResult> Users(
        [FromQuery] string? role,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _admin.ListUsersAsync(role, search, page, pageSize, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(string id, CancellationToken cancellationToken)
    {
        var result = await _admin.GetUserAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("users/{id}/block")]
    public async Task<IActionResult> Block(string id, CancellationToken cancellationToken)
    {
        var result = await _admin.SetBlockedAsync(AdminId, id, blocked: true, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPost("users/{id}/unblock")]
    public async Task<IActionResult> Unblock(string id, CancellationToken cancellationToken)
    {
        var result = await _admin.SetBlockedAsync(AdminId, id, blocked: false, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("reviews/{id}")]
    public async Task<IActionResult> DeleteReview(string id, CancellationToken cancellationToken)
    {
        var result = await _admin.DeleteReviewAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken)
    {
        var stats = await _admin.GetStatsAsync(cancellationToken);
        return Ok(stats);
    }
}

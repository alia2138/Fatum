using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Reviews;

namespace TaroPlatform.Api.Controllers;

/// <summary>
/// Відгуки про консультації та відповіді таролога. Рейтинг таролога рахується автоматично.
/// </summary>
[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    /// <summary>Залишити відгук на проведену консультацію (1–5 зірок).</summary>
    [HttpPost]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Create([FromBody] ReviewCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _reviews.CreateAsync(UserId, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Змінити свій відгук протягом 14 днів після публікації.</summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Update(string id, [FromBody] ReviewUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _reviews.UpdateAsync(UserId, id, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Відповідь таролога на відгук (один раз).</summary>
    [HttpPost("{id}/reply")]
    [Authorize(Roles = "Consultant")]
    public async Task<IActionResult> Reply(string id, [FromBody] ReviewReplyRequest request, CancellationToken cancellationToken)
    {
        var result = await _reviews.ReplyAsync(UserId, id, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Відгуки таролога з пагінацією: GET /api/reviews/consultant/{id}?page=1&amp;pageSize=20</summary>
    [HttpGet("consultant/{consultantId}")]
    public async Task<IActionResult> ForConsultant(string consultantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _reviews.ListForConsultantAsync(consultantId, page, pageSize, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}

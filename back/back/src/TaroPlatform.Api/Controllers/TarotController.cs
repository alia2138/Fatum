using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Tarot;

namespace TaroPlatform.Api.Controllers;

/// <summary>
/// Гадання на Таро. Доступ лише для клієнтів (роль Client).
/// Кожен розклад — окрема сесія, яка зберігається в історії.
/// </summary>
[ApiController]
[Route("api/tarot")]
[Authorize(Roles = "Client")]
public class TarotController : ControllerBase
{
    private readonly ITarotService _tarot;

    public TarotController(ITarotService tarot)
    {
        _tarot = tarot;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    [HttpGet("topics")]
    public IActionResult GetTopics() => Ok(_tarot.GetTopics());

    [HttpPost("readings")]
    public async Task<IActionResult> Start([FromBody] StartReadingRequest request, CancellationToken cancellationToken)
    {
        var result = await _tarot.StartAsync(UserId, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("readings")]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var history = await _tarot.GetHistoryAsync(UserId, cancellationToken);
        return Ok(history);
    }

    [HttpGet("readings/{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var result = await _tarot.GetAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("readings/{id}/messages")]
    public async Task<IActionResult> Ask(string id, [FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        var result = await _tarot.AskAsync(UserId, id, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("readings/{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var result = await _tarot.DeleteAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return NoContent();
    }
}

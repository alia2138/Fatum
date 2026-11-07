using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Catalog;
using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Api.Controllers;

/// <summary>
/// Каталог таро-консультантів, їхні профілі, графік і вільні слоти.
/// </summary>
[ApiController]
[Route("api/consultants")]
[Authorize]
public class ConsultantsController : ControllerBase
{
    private readonly IConsultantService _consultants;

    public ConsultantsController(IConsultantService consultants)
    {
        _consultants = consultants;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    /// <summary>Каталог з пошуком і фільтрами. sortBy: price, experience, name.</summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] ReadingTopic? topic,
        [FromQuery] string? language,
        [FromQuery] int? minPrice,
        [FromQuery] int? maxPrice,
        [FromQuery] int? minExperience,
        [FromQuery] string sortBy = "price",
        [FromQuery] bool desc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ConsultantSearch(search, topic, language, minPrice, maxPrice, minExperience, sortBy, desc, page, pageSize);
        var result = await _consultants.SearchAsync(query, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Profile(string id, CancellationToken cancellationToken)
    {
        var result = await _consultants.GetPublicProfileAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Вільні слоти таролога на дату, наприклад ?date=2026-11-02.</summary>
    [HttpGet("{id}/slots")]
    public async Task<IActionResult> Slots(string id, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _consultants.GetSlotsAsync(id, date, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Публічний графік таролога: показує клієнтам, у які дні він працює.</summary>
    [HttpGet("{id}/schedule")]
    public async Task<IActionResult> PublicSchedule(string id, CancellationToken cancellationToken)
    {
        var result = await _consultants.GetScheduleAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("me/schedule")]
    [Authorize(Roles = "Consultant")]
    public async Task<IActionResult> GetMySchedule(CancellationToken cancellationToken)
    {
        var result = await _consultants.GetScheduleAsync(UserId, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Замінює весь тижневий графік. Час у форматі HH:mm, день — англійською (Monday).</summary>
    [HttpPut("me/schedule")]
    [Authorize(Roles = "Consultant")]
    public async Task<IActionResult> UpdateMySchedule([FromBody] UpdateScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await _consultants.UpdateScheduleAsync(UserId, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Catalog;
using TaroPlatform.Domain.Consultations;

namespace TaroPlatform.Api.Controllers;

/// <summary>Робочий кабінет таролога: історія графіків, звіт про години і список консультацій.</summary>
[ApiController]
[Route("api/consultants/me")]
[Authorize(Roles = "Consultant")]
public class ConsultantWorkController : ControllerBase
{
    private readonly IConsultantWorkService _work;

    public ConsultantWorkController(IConsultantWorkService work)
    {
        _work = work;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    /// <summary>Поточний і попередні графіки з періодами, коли вони діяли.</summary>
    [HttpGet("schedule/history")]
    public async Task<IActionResult> ScheduleHistory(CancellationToken cancellationToken)
    {
        var result = await _work.GetScheduleHistoryAsync(UserId, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Години за період: доступні, заброньовані, проведені. Параметри from і to у форматі yyyy-MM-dd.</summary>
    [HttpGet("work-summary")]
    public async Task<IActionResult> WorkSummary([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
    {
        var result = await _work.GetWorkSummaryAsync(UserId, from, to, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Консультації за період. Фільтр status: Pending, Confirmed, Completed, Declined тощо.</summary>
    [HttpGet("consultations")]
    public async Task<IActionResult> Consultations(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] ConsultationStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _work.GetConsultationsAsync(UserId, from, to, status, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}

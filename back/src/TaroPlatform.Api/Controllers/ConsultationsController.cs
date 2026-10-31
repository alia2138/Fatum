using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Consultations;

namespace TaroPlatform.Api.Controllers;

/// <summary>
/// Заявки на консультації. Клієнт записується, таролог підтверджує або відхиляє.
/// </summary>
[ApiController]
[Route("api/consultations")]
[Authorize]
public class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultations;

    public ConsultationsController(IConsultationService consultations)
    {
        _consultations = consultations;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    /// <summary>Записатися на вільний слот (startUtc з /api/consultants/{id}/slots).</summary>
    [HttpPost]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Book([FromBody] BookConsultationRequest request, CancellationToken cancellationToken)
    {
        var result = await _consultations.BookAsync(UserId, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Мої заявки: для клієнта — його записи, для таролога — заявки до нього.</summary>
    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var list = await _consultations.GetMineAsync(UserId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var result = await _consultations.GetAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Consultant")]
    public async Task<IActionResult> Confirm(string id, [FromBody] ConfirmConsultationRequest? request, CancellationToken cancellationToken)
    {
        var result = await _consultations.ConfirmAsync(UserId, id, request ?? new ConfirmConsultationRequest(null), cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("{id}/decline")]
    [Authorize(Roles = "Consultant")]
    public async Task<IActionResult> Decline(string id, [FromBody] DeclineConsultationRequest? request, CancellationToken cancellationToken)
    {
        var result = await _consultations.DeclineAsync(UserId, id, request ?? new DeclineConsultationRequest(null), cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, CancellationToken cancellationToken)
    {
        var result = await _consultations.CancelAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Consultant")]
    public async Task<IActionResult> Complete(string id, CancellationToken cancellationToken)
    {
        var result = await _consultations.CompleteAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Matrix;

namespace TaroPlatform.Api.Controllers;

/// <summary>
/// Збережені матриці: історія, відповіді по зонах і розмова з AI про матрицю.
/// </summary>
[ApiController]
[Route("api/matrix/saved")]
[Authorize]
public class MatrixHistoryController : ControllerBase
{
    private readonly ISavedMatrixService _saved;

    public MatrixHistoryController(ISavedMatrixService saved)
    {
        _saved = saved;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var list = await _saved.ListAsync(UserId, cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveMatrixRequest request, CancellationToken cancellationToken)
    {
        var result = await _saved.SaveAsync(UserId, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var result = await _saved.GetAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Відповіді по всіх зонах: комфорт, таланти, фінанси, стосунки, родові лінії тощо.</summary>
    [HttpGet("{id}/zones")]
    public async Task<IActionResult> Zones(string id, CancellationToken cancellationToken)
    {
        var result = await _saved.GetZonesAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Питання про матрицю. zoneKey необов'язковий, наприклад "finance".</summary>
    [HttpPost("{id}/questions")]
    public async Task<IActionResult> Ask(string id, [FromBody] MatrixQuestionRequest request, CancellationToken cancellationToken)
    {
        var result = await _saved.AskAsync(UserId, id, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Перейменувати матрицю. Порожня назва прибирає її.</summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> Rename(string id, [FromBody] RenameMatrixRequest request, CancellationToken cancellationToken)
    {
        var result = await _saved.RenameAsync(UserId, id, request.Label, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Очистити всі питання до AI по цій матриці (саму матрицю залишити).</summary>
    [HttpDelete("{id}/messages")]
    public async Task<IActionResult> ClearQuestions(string id, CancellationToken cancellationToken)
    {
        var result = await _saved.ClearConversationAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var result = await _saved.DeleteAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return NoContent();
    }
}

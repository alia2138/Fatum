using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Matrix;

namespace TaroPlatform.Api.Controllers;

/// <summary>
/// Матриця долі: розрахунок, трактування і сумісність. Дати у форматі yyyy-MM-dd.
/// </summary>
[ApiController]
[Route("api/matrix")]
[Authorize]
public class MatrixController : ControllerBase
{
    private readonly IMatrixService _matrix;

    public MatrixController(IMatrixService matrix)
    {
        _matrix = matrix;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    /// <summary>Матриця долі клієнта за його датою народження з профілю.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _matrix.GetMineAsync(UserId, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Розрахунок матриці за будь-якою датою народження.</summary>
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculateMatrixRequest request, CancellationToken cancellationToken)
    {
        var result = await _matrix.CalculateAsync(request.BirthDate, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>Сумісність за двома датами народження.</summary>
    [HttpPost("compatibility")]
    public async Task<IActionResult> Compatibility([FromBody] CompatibilityRequest request, CancellationToken cancellationToken)
    {
        var result = await _matrix.CompatibilityAsync(request.FirstBirthDate, request.SecondBirthDate, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}

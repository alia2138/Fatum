using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Profile;

namespace TaroPlatform.Api.Controllers;

/// <summary>
/// Профіль користувача. Усі поля редагуються частково (PATCH):
/// передаєте лише ті, які змінюєте.
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profiles;

    public ProfileController(IProfileService profiles)
    {
        _profiles = profiles;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _profiles.GetMeAsync(UserId, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPatch("client")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> UpdateClient([FromBody] ClientProfileUpdate request, CancellationToken cancellationToken)
    {
        var result = await _profiles.UpdateClientAsync(UserId, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPatch("consultant")]
    [Authorize(Roles = "Consultant")]
    public async Task<IActionResult> UpdateConsultant([FromBody] ConsultantProfileUpdate request, CancellationToken cancellationToken)
    {
        var result = await _profiles.UpdateConsultantAsync(UserId, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Файл не передано." });

        await using var stream = file.OpenReadStream();
        var result = await _profiles.UploadPhotoAsync(UserId, stream, file.ContentType, file.Length, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("photo")]
    public async Task<IActionResult> DeletePhoto(CancellationToken cancellationToken)
    {
        var result = await _profiles.DeletePhotoAsync(UserId, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}

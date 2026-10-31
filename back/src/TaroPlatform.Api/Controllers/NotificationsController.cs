using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Notifications;

namespace TaroPlatform.Api.Controllers;

/// <summary>Сповіщення всередині застосунку.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var list = await _notifications.GetMineAsync(UserId, cancellationToken);
        return Ok(list);
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(string id, CancellationToken cancellationToken)
    {
        var result = await _notifications.MarkReadAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return NoContent();
    }
}

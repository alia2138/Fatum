using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Chat;

namespace TaroPlatform.Api.Controllers;

/// <summary>Чати між клієнтом і таро-консультантом.</summary>
[ApiController]
[Route("api/chats")]
[Authorize]
public class ChatsController : ControllerBase
{
    private readonly IChatService _chat;

    public ChatsController(IChatService chat)
    {
        _chat = chat;
    }

    private string UserId =>
        User.FindFirstValue("sub") ?? throw new InvalidOperationException("Немає ідентифікатора користувача.");

    [HttpGet]
    public async Task<IActionResult> Conversations(CancellationToken cancellationToken)
    {
        var list = await _chat.GetConversationsAsync(UserId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> Messages(string id, CancellationToken cancellationToken)
    {
        var result = await _chat.GetMessagesAsync(UserId, id, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> Send(string id, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await _chat.SendTextAsync(UserId, id, request.Text, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}

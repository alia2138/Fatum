using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaroPlatform.Application.Auth;
using TaroPlatform.Domain.Enums;

namespace TaroPlatform.Api.Controllers;

/// <summary>
/// Вхід через Google і GitHub (OAuth 2.0).
/// 1) GET /api/auth/external/login/google?role=Client&amp;birthDate=1995-05-20
/// 2) Провайдер повертає користувача на /signin-google, далі на /api/auth/external/complete
/// 3) complete видає JWT, такий самий, як при звичайному логіні.
/// </summary>
[ApiController]
[Route("api/auth/external")]
public class ExternalAuthController : ControllerBase
{
    private const string ExternalScheme = "External";

    private readonly IAuthService _auth;

    public ExternalAuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpGet("login/{provider}")]
    public IActionResult Login(
        string provider,
        [FromQuery] UserRole role = UserRole.Client,
        [FromQuery] DateTime? birthDate = null)
    {
        var name = provider.ToLowerInvariant();
        var scheme = name switch
        {
            "google" => GoogleDefaults.AuthenticationScheme,
            "github" => GitHubAuthenticationDefaults.AuthenticationScheme,
            _ => null
        };

        if (scheme is null)
            return BadRequest(new { error = "Невідомий провайдер. Доступні: google, github." });

        // Зберігаємо роль і дату народження, щоб вони дійшли до callback
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Complete))
        };
        properties.Items["provider"] = name;
        properties.Items["role"] = role.ToString();
        if (birthDate is not null)
            properties.Items["birthDate"] = birthDate.Value.ToString("O");

        return Challenge(properties, scheme);
    }

    [HttpGet("complete")]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(ExternalScheme);
        if (!result.Succeeded || result.Principal is null || result.Properties is null)
            return BadRequest(new { error = "Вхід через соцмережу не підтверджено." });

        var items = result.Properties.Items;
        var provider = items.TryGetValue("provider", out var p) ? p ?? string.Empty : string.Empty;
        var role = items.TryGetValue("role", out var r) && Enum.TryParse<UserRole>(r, out var parsedRole)
            ? parsedRole
            : UserRole.Client;
        DateTime? birthDate = items.TryGetValue("birthDate", out var b) && DateTime.TryParse(b, out var date)
            ? date
            : null;

        var providerUserId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = result.Principal.FindFirstValue(ClaimTypes.Email);

        // Тимчасове cookie більше не потрібне
        await HttpContext.SignOutAsync(ExternalScheme);

        if (string.IsNullOrEmpty(providerUserId) || string.IsNullOrEmpty(provider))
            return BadRequest(new { error = "Провайдер не повернув ідентифікатор користувача." });

        var identity = new ExternalIdentity(provider, providerUserId, email, role, birthDate);
        var login = await _auth.ExternalLoginAsync(identity, cancellationToken);
        if (login.IsFailure)
            return BadRequest(new { error = login.Error });

        return Ok(login.Value);
    }
}

using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using TaroPlatform.Application.Auth;
using TaroPlatform.Domain.Enums;

namespace TaroPlatform.Api.Controllers;

/// <summary>
/// Вхід через Google і GitHub (OAuth 2.0).
/// 1) GET /api/auth/external/login/google?role=Client&amp;birthDate=1995-05-20&amp;returnUrl=https://site/html/external.html
///    returnUrl — куди повернути користувача після входу (сторінка external.html того фронту, звідки він прийшов).
///    Приймається лише з origin зі списку Frontend:AllowedOrigins, інакше береться Frontend:CallbackUrl.
/// 2) Провайдер повертає користувача на /signin-google, далі на /api/auth/external/complete
/// 3) complete перенаправляє на фронт із токеном у фрагменті URL.
/// </summary>
[ApiController]
[Route("api/auth/external")]
public class ExternalAuthController : ControllerBase
{
    private const string ExternalScheme = "External";

    private readonly IAuthService _auth;
    private readonly string _frontendCallbackUrl;
    private readonly string[] _allowedOrigins;

    public ExternalAuthController(IAuthService auth, IConfiguration config)
    {
        _auth = auth;
        _frontendCallbackUrl = config["Frontend:CallbackUrl"]
            ?? "http://localhost:63342/front/dist/html/external.html";

        // Куди дозволено повертати токен: ті самі origin, що й для CORS, плюс origin із CallbackUrl.
        var configured = config.GetSection("Frontend:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:63342" };
        _allowedOrigins = configured
            .Append(OriginOf(_frontendCallbackUrl) ?? string.Empty)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim().TrimEnd('/'))
            .ToArray();
    }

    [HttpGet("login/{provider}")]
    public IActionResult Login(
        string provider,
        [FromQuery] UserRole role = UserRole.Client,
        [FromQuery] DateTime? birthDate = null,
        [FromQuery] string? returnUrl = null)
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

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Complete))
        };
        properties.Items["provider"] = name;
        properties.Items["role"] = role.ToString();
        if (birthDate is not null)
            properties.Items["birthDate"] = birthDate.Value.ToString("O");

        var safeReturnUrl = SafeReturnUrl(returnUrl);
        if (safeReturnUrl is not null)
            properties.Items["returnUrl"] = safeReturnUrl;

        return Challenge(properties, scheme);
    }

    [HttpGet("complete")]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(ExternalScheme);
        if (!result.Succeeded || result.Principal is null || result.Properties is null)
            return RedirectToFrontend(error: "Вхід через соцмережу не підтверджено.");

        var items = result.Properties.Items;
        var provider = items.TryGetValue("provider", out var p) ? p ?? string.Empty : string.Empty;
        var role = items.TryGetValue("role", out var r) && Enum.TryParse<UserRole>(r, out var parsedRole)
            ? parsedRole
            : UserRole.Client;
        DateTime? birthDate = items.TryGetValue("birthDate", out var b) && DateTime.TryParse(b, out var date)
            ? date
            : null;
        var returnUrl = items.TryGetValue("returnUrl", out var ru) ? SafeReturnUrl(ru) : null;

        var providerUserId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = result.Principal.FindFirstValue(ClaimTypes.Email);

        await HttpContext.SignOutAsync(ExternalScheme);

        if (string.IsNullOrEmpty(providerUserId) || string.IsNullOrEmpty(provider))
            return RedirectToFrontend(error: "Провайдер не повернув ідентифікатор користувача.", returnUrl: returnUrl);

        var identity = new ExternalIdentity(provider, providerUserId, email, role, birthDate);
        var login = await _auth.ExternalLoginAsync(identity, cancellationToken);
        if (login.IsFailure)
            return RedirectToFrontend(error: login.Error, returnUrl: returnUrl);

        var session = login.Value;
        return RedirectToFrontend(
            token: session.Token,
            userId: session.UserId,
            email: session.Email,
            role: session.Role,
            returnUrl: returnUrl);
    }

    /// <summary>
    /// Дані передаються у фрагменті (#...), тому вони не потрапляють на сервер і в логи.
    /// </summary>
    private IActionResult RedirectToFrontend(
        string? error = null,
        string? token = null,
        string? userId = null,
        string? email = null,
        string? role = null,
        string? returnUrl = null)
    {
        var fragment = error is not null
            ? $"error={Uri.EscapeDataString(error)}"
            : $"token={Uri.EscapeDataString(token ?? string.Empty)}" +
              $"&userId={Uri.EscapeDataString(userId ?? string.Empty)}" +
              $"&email={Uri.EscapeDataString(email ?? string.Empty)}" +
              $"&role={Uri.EscapeDataString(role ?? string.Empty)}";

        return Redirect($"{returnUrl ?? _frontendCallbackUrl}?{fragment}");
    }

    /// <summary>
    /// Повертає returnUrl без query і фрагмента, якщо це http(s)-адреса дозволеного origin. Інакше null.
    /// Білий список потрібен, щоб токен не можна було переслати на сторонній сайт.
    /// </summary>
    private string? SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        var origin = uri.GetLeftPart(UriPartial.Authority);
        if (!_allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            return null;

        return uri.GetLeftPart(UriPartial.Path);
    }

    private static string? OriginOf(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;
}

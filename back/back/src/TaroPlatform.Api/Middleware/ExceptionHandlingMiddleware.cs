using System.Net;

namespace TaroPlatform.Api.Middleware;

/// <summary>
/// Middleware у ланцюжку обробки запиту (патерн Chain of Responsibility).
/// Перехоплює необроблені винятки і повертає єдиний JSON-формат помилки.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необроблена помилка під час обробки запиту");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Внутрішня помилка сервера"
            });
        }
    }
}

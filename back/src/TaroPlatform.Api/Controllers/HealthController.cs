using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Infrastructure.AI;
using TaroPlatform.Infrastructure.Persistence;

namespace TaroPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MongoContext _mongo;
    private readonly IDateTimeProvider _clock;
    private readonly AiSettings _ai;

    public HealthController(MongoContext mongo, IDateTimeProvider clock, IOptions<AiSettings> ai)
    {
        _mongo = mongo;
        _clock = clock;
        _ai = ai.Value;
    }

    /// <summary>Стан сервера, бази і AI. Ключ API не показується, лише те, чи він заданий.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var mongoOk = await _mongo.PingAsync(cancellationToken);

        return Ok(new
        {
            status = "ok",
            mongo = mongoOk ? "connected" : "unavailable",
            time = _clock.UtcNow,
            ai = new
            {
                enabled = _ai.Enabled,
                keySet = !string.IsNullOrWhiteSpace(_ai.ApiKey),
                configured = _ai.IsConfigured,
                baseUrl = _ai.BaseUrl,
                model = _ai.Model
            }
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Infrastructure.Persistence;

namespace TaroPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MongoContext _mongo;
    private readonly IDateTimeProvider _clock;

    public HealthController(MongoContext mongo, IDateTimeProvider clock)
    {
        _mongo = mongo;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var mongoOk = await _mongo.PingAsync(cancellationToken);

        return Ok(new
        {
            status = "ok",
            mongo = mongoOk ? "connected" : "unavailable",
            time = _clock.UtcNow
        });
    }
}

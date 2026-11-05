using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Application.Demo;

namespace TaroPlatform.Api.Controllers;

/// <summary>Тестові дані для бази. Доступно лише адміністратору, запуск один раз.</summary>
[ApiController]
[Route("api/admin/demo")]
[Authorize(Roles = "Admin")]
public class DemoController : ControllerBase
{
    private readonly DemoDataSeeder _seeder;

    public DemoController(DemoDataSeeder seeder)
    {
        _seeder = seeder;
    }

    /// <summary>Створює 24 тарологи і 76 клієнтів (пароль 123456) з гаданнями, консультаціями, чатами і відгуками.</summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        var result = await _seeder.SeedAsync(cancellationToken);
        return Ok(result);
    }
}

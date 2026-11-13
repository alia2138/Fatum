using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaroPlatform.Domain.Tarot;
using TaroPlatform.Infrastructure.Tarot;

namespace TaroPlatform.Api.Controllers;

public record TarotCardDto(int Id, string Name, string ArcanaType, string ImageUrl);

/// <summary>
/// Колода з зображеннями. Картинки публічні, тому доступ без входу.
/// Якщо в wwwroot/cards є файл {id}.png, .jpg або .webp, він віддається замість згенерованого.
/// </summary>
[ApiController]
[Route("api/tarot/cards")]
[AllowAnonymous]
public class TarotCardsController : ControllerBase
{
    private static readonly string[] Extensions = { ".png", ".jpg", ".webp" };

    private readonly IWebHostEnvironment _env;

    public TarotCardsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public IActionResult All()
    {
        var cards = TarotDeck.All
            .Select(c => new TarotCardDto(c.Id, c.Name, c.ArcanaType, ImageUrl(c.Id)))
            .ToList();

        return Ok(cards);
    }

    [HttpGet("{id:int}/image")]
    public IActionResult Image(int id)
    {
        if (id < 0 || id > 77)
            return NotFound();

        var folder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "cards");
        foreach (var ext in Extensions)
        {
            var path = Path.Combine(folder, $"{id}{ext}");
            if (System.IO.File.Exists(path))
            {
                var type = ext == ".png" ? "image/png" : ext == ".jpg" ? "image/jpeg" : "image/webp";
                return PhysicalFile(path, type);
            }
        }

        var card = TarotDeck.GetById(id);
        return Content(CardArtGenerator.Svg(card), "image/svg+xml");
    }

    private static string ImageUrl(int id) => $"/api/tarot/cards/{id}/image";
}

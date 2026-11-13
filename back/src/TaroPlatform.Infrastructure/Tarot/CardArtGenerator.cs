using System.Security;
using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Infrastructure.Tarot;

/// <summary>
/// Створює оригінальне зображення карти у форматі SVG: рамка, колір за мастю,
/// номер або масть і назва. Це наші власні графічні елементи, без чужих ілюстрацій.
/// </summary>
public static class CardArtGenerator
{
    public static string Svg(TarotCard card)
    {
        var (background, accent) = Palette(card);
        var name = SecurityElement.Escape(card.Name);
        var label = card is MajorArcana
            ? card.Id.ToString()
            : SuitSymbol(((MinorArcana)card).Suit);

        return $"""
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 500" width="300" height="500">
  <rect x="6" y="6" width="288" height="488" rx="18" fill="{background}" stroke="{accent}" stroke-width="6"/>
  <rect x="22" y="22" width="256" height="456" rx="12" fill="none" stroke="{accent}" stroke-width="1.5" opacity="0.6"/>
  <circle cx="150" cy="210" r="88" fill="none" stroke="{accent}" stroke-width="3"/>
  <circle cx="150" cy="210" r="60" fill="{accent}" opacity="0.15"/>
  <text x="150" y="232" text-anchor="middle" font-family="Georgia, serif" font-size="72" fill="{accent}">{label}</text>
  <text x="150" y="380" text-anchor="middle" font-family="Georgia, serif" font-size="26" fill="{accent}">{name}</text>
  <text x="150" y="420" text-anchor="middle" font-family="Arial, sans-serif" font-size="14" fill="{accent}" opacity="0.8">{SecurityElement.Escape(card.ArcanaType)}</text>
</svg>
""";
    }

    private static (string Background, string Accent) Palette(TarotCard card) =>
        card is MinorArcana minor
            ? minor.Suit switch
            {
                Suit.Wands => ("#7a2e1d", "#f2a03d"),
                Suit.Cups => ("#1d3f7a", "#7fd1f0"),
                Suit.Swords => ("#3a3f47", "#d9e2ec"),
                _ => ("#2e5a2c", "#c9e07a")
            }
            : ("#3b2a6b", "#e0b84c");

    private static string SuitSymbol(Suit suit) => suit switch
    {
        Suit.Wands => "Ж",
        Suit.Cups => "К",
        Suit.Swords => "М",
        _ => "П"
    };
}

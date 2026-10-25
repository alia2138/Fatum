namespace TaroPlatform.Domain.Tarot;

/// <summary>Молодший аркан: 56 карт, розділених на чотири масті.</summary>
public class MinorArcana : TarotCard
{
    public MinorArcana(int id, Suit suit, string name, string uprightMeaning, string reversedMeaning)
        : base(id, name, uprightMeaning, reversedMeaning)
    {
        Suit = suit;
    }

    public Suit Suit { get; }

    public override string ArcanaType => "Молодший аркан";
}

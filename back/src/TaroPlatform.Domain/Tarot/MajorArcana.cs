namespace TaroPlatform.Domain.Tarot;

/// <summary>Старший аркан: 22 карти з великими символами долі.</summary>
public class MajorArcana : TarotCard
{
    public MajorArcana(int id, string name, string uprightMeaning, string reversedMeaning)
        : base(id, name, uprightMeaning, reversedMeaning)
    {
    }

    public override string ArcanaType => "Старший аркан";
}

namespace TaroPlatform.Domain.Tarot;

/// <summary>
/// Базова карта Таро. Містить значення в прямому та перевернутому положенні.
/// Конкретні типи (старші та молодші аркани) успадковують цей клас.
/// </summary>
public abstract class TarotCard
{
    protected TarotCard(int id, string name, string uprightMeaning, string reversedMeaning)
    {
        Id = id;
        Name = name;
        UprightMeaning = uprightMeaning;
        ReversedMeaning = reversedMeaning;
    }

    public int Id { get; }
    public string Name { get; }
    public string UprightMeaning { get; }
    public string ReversedMeaning { get; }

    public abstract string ArcanaType { get; }

    public string GetMeaning(bool isReversed) => isReversed ? ReversedMeaning : UprightMeaning;
}

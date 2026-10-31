namespace TaroPlatform.Domain.Matrix;

/// <summary>
/// Розрахована матриця долі: значення всіх точок за датою народження.
/// Значення від 1 до 22 відповідають старшим арканам.
/// </summary>
public class DestinyMatrix
{
    public DestinyMatrix(DateOnly birthDate, IReadOnlyDictionary<string, int> points)
    {
        BirthDate = birthDate;
        Points = points;
    }

    public DateOnly BirthDate { get; }
    public IReadOnlyDictionary<string, int> Points { get; }

    public int this[string key] => Points[key];
}

/// <summary>Період віку з 2,5 року: значення і межі.</summary>
public record AgePeriod(double From, double To, int Value);

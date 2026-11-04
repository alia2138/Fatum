using TaroPlatform.Domain.Matrix;

namespace TaroPlatform.Application.Matrix;

/// <summary>Перетворює розраховану матрицю на DTO. Без AI, детерміновано.</summary>
public static class MatrixDescriber
{
    public static IReadOnlyList<MatrixPointDto> Points(DestinyMatrix matrix) =>
        DestinyMatrixCalculator.PointDefinitions
            .Where(d => matrix.Points.ContainsKey(d.Key))
            .Select(d => Point(d.Key, matrix))
            .ToList();

    public static MatrixPointDto Point(string key, DestinyMatrix matrix)
    {
        var definition = DestinyMatrixCalculator.PointDefinitions.First(p => p.Key == key);
        var value = matrix[key];
        var arcana = DestinyMatrixCalculator.ArcanaFor(value);
        return new MatrixPointDto(key, definition.Title, definition.Group, value, arcana.Name, arcana.UprightMeaning);
    }

    public static IReadOnlyList<TimelinePeriodDto> Timeline(DestinyMatrix matrix) =>
        DestinyMatrixCalculator.Timeline(matrix)
            .Select(p =>
            {
                var arcana = DestinyMatrixCalculator.ArcanaFor(p.Value);
                return new TimelinePeriodDto(p.From, p.To, p.Value, arcana.Name, DestinyMatrixCalculator.LineFor(p.From));
            })
            .ToList();

    public static CurrentPeriodDto Current(DestinyMatrix matrix, DateOnly birthDate, DateOnly today)
    {
        var ageYears = (today.DayNumber - birthDate.DayNumber) / 365.25;
        var period = DestinyMatrixCalculator.PeriodAt(matrix, ageYears);
        var arcana = DestinyMatrixCalculator.ArcanaFor(period.Value);

        return new CurrentPeriodDto(
            Math.Round(ageYears, 1),
            period.From,
            period.To,
            period.Value,
            arcana.Name,
            arcana.UprightMeaning,
            DestinyMatrixCalculator.LineFor(period.From));
    }
}

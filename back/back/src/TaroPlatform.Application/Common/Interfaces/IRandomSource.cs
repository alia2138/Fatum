namespace TaroPlatform.Application.Common.Interfaces;

/// <summary>Джерело випадковості. Підміняється у тестах, щоб результат був передбачуваним.</summary>
public interface IRandomSource
{
    int Next(int maxExclusive);
}

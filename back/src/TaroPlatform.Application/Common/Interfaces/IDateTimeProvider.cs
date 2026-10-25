namespace TaroPlatform.Application.Common.Interfaces;

/// <summary>
/// Абстракція часу. Дає змогу підміняти час у тестах.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

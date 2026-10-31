namespace TaroPlatform.Domain.Tarot;

/// <summary>Позиція в розкладі, наприклад «Минуле» або «Порада».</summary>
public record SpreadPosition(int Index, string Name);

/// <summary>
/// Опис розкладу для теми: назва, передісторія та позиції (кількість позицій = кількість карт).
/// </summary>
public record SpreadDefinition(
    ReadingTopic Topic,
    string Title,
    string Story,
    IReadOnlyList<SpreadPosition> Positions);

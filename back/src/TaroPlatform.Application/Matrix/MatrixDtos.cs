using TaroPlatform.Domain.Matrix;

namespace TaroPlatform.Application.Matrix;

/// <summary>Method не обов'язковий: за замовчуванням використовується спосіб із відео.</summary>
public record CalculateMatrixRequest(DateOnly BirthDate, ChannelMethod? Method);

public record CompatibilityRequest(DateOnly FirstBirthDate, DateOnly SecondBirthDate, ChannelMethod? Method);

public record MatrixPointDto(
    string Key,
    string Title,
    string Group,
    int Value,
    string Arcana,
    string ArcanaMeaning);

public record TimelinePeriodDto(
    double FromAge,
    double ToAge,
    int Value,
    string Arcana,
    string Line);

public record CurrentPeriodDto(
    double AgeYears,
    double FromAge,
    double ToAge,
    int Value,
    string Arcana,
    string ArcanaMeaning,
    string Line);

public record MatrixResultDto(
    DateOnly BirthDate,
    string Method,
    IReadOnlyList<MatrixPointDto> Points,
    IReadOnlyList<TimelinePeriodDto> Timeline,
    CurrentPeriodDto Current,
    string Interpretation,
    string InterpretationSource);

public record CompatibilityPointDto(
    string Key,
    string Title,
    int Value,
    string Arcana,
    string ArcanaMeaning);

public record CompatibilityResultDto(
    DateOnly FirstBirthDate,
    DateOnly SecondBirthDate,
    string Method,
    IReadOnlyList<CompatibilityPointDto> Points,
    string Interpretation,
    string InterpretationSource);

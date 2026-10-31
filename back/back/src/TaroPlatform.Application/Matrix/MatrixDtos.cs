namespace TaroPlatform.Application.Matrix;

public record CalculateMatrixRequest(DateOnly BirthDate);

public record CompatibilityRequest(DateOnly FirstBirthDate, DateOnly SecondBirthDate);

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
    IReadOnlyList<CompatibilityPointDto> Points,
    string Interpretation,
    string InterpretationSource);

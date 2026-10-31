using TaroPlatform.Domain.Matrix;

namespace TaroPlatform.Application.Matrix;

public record SaveMatrixRequest(DateOnly BirthDate, ChannelMethod? Method, string? Label);

/// <summary>ZoneKey необов'язковий: якщо вказано, відповідь фокусується на зоні.</summary>
public record MatrixQuestionRequest(string Question, string? ZoneKey);

public record SavedMatrixSummaryDto(
    string Id,
    DateOnly BirthDate,
    string Method,
    string? Label,
    DateTime CreatedAt,
    int QuestionsCount);

public record MatrixMessageDto(string Role, string Text, string? Zone, string Provider, DateTime CreatedAt);

public record SavedMatrixDetailDto(
    string Id,
    DateOnly BirthDate,
    string Method,
    string? Label,
    DateTime CreatedAt,
    IReadOnlyList<MatrixPointDto> Points,
    IReadOnlyList<TimelinePeriodDto> Timeline,
    CurrentPeriodDto Current,
    IReadOnlyList<MatrixMessageDto> Messages);

/// <summary>Відповідь по зоні: точки зони і текст, складений з їхніх значень.</summary>
public record ZoneAnswerDto(
    string ZoneKey,
    string Title,
    string Description,
    IReadOnlyList<MatrixPointDto> Points,
    string Text);

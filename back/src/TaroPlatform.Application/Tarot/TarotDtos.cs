using TaroPlatform.Domain.Tarot;

namespace TaroPlatform.Application.Tarot;

public record TopicDto(string Key, string Title, string Story, IReadOnlyList<string> Positions);

/// <summary>Передісторію пише користувач, щоб AI розумів його ситуацію.</summary>
public record StartReadingRequest(ReadingTopic Topic, string Background, string Question);

public record AskRequest(string Question);

public record ReadingSummaryDto(
    string Id,
    string Topic,
    string Title,
    string Question,
    DateTime CreatedAt,
    int CardsCount);

/// <summary>Round: 0 — карта першого розкладу, 1+ — карта додаткового питання.</summary>
public record DrawnCardDto(
    int Position,
    string PositionName,
    string CardName,
    string ArcanaType,
    bool IsReversed,
    string Meaning,
    int Round);

public record MessageDto(string Role, string Text, string Provider, DateTime CreatedAt);

public record ReadingDetailDto(
    string Id,
    string Topic,
    string Title,
    string Background,
    string Question,
    DateTime CreatedAt,
    IReadOnlyList<DrawnCardDto> Cards,
    IReadOnlyList<MessageDto> Messages);

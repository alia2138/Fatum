namespace TaroPlatform.Application.Reviews;

public record ReviewCreateRequest(string ConsultationId, int Rating, string? Text);

/// <summary>Null означає «не змінювати». Порожній текст очищає відгук.</summary>
public record ReviewUpdateRequest(int? Rating, string? Text);

public record ReviewReplyRequest(string Text);

public record ReviewDto(
    string Id,
    string ConsultationId,
    string ClientName,
    int Rating,
    string? Text,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? ConsultantReply,
    DateTime? RepliedAt);

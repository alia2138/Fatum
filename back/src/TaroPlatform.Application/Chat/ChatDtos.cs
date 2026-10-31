namespace TaroPlatform.Application.Chat;

public record ConversationDto(string Id, string OtherPartyId, string OtherPartyName, DateTime LastMessageAt);

public record MessageDto(string Kind, string? SenderId, string Text, string? Link, DateTime CreatedAt);

public record SendMessageRequest(string Text);

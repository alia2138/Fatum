using TaroPlatform.Application.Common.Interfaces;
using TaroPlatform.Application.Common.Results;
using TaroPlatform.Domain.Chat;

namespace TaroPlatform.Application.Chat;

public interface IChatService
{
    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MessageDto>>> GetMessagesAsync(string userId, string conversationId, CancellationToken cancellationToken = default);
    Task<Result<MessageDto>> SendTextAsync(string userId, string conversationId, string text, CancellationToken cancellationToken = default);
    Task<string> OpenConversationAsync(string clientId, string consultantId, CancellationToken cancellationToken = default);
    Task PostSystemMessageAsync(string conversationId, string text, string? link, CancellationToken cancellationToken = default);
}

public class ChatService : IChatService
{
    private const int MaxTextLength = 1000;

    private readonly IConversationRepository _conversations;
    private readonly IUserRepository _users;

    public ChatService(IConversationRepository conversations, IUserRepository users)
    {
        _conversations = conversations;
        _users = users;
    }

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var conversations = await _conversations.GetByParticipantAsync(userId, cancellationToken);
        var result = new List<ConversationDto>();

        foreach (var conversation in conversations.OrderByDescending(c => c.LastMessageAt))
        {
            var otherId = conversation.ClientId == userId ? conversation.ConsultantId : conversation.ClientId;
            var other = await _users.GetByIdAsync(otherId, cancellationToken);
            result.Add(new ConversationDto(
                conversation.Id,
                otherId,
                other?.DisplayName ?? "Користувач",
                conversation.LastMessageAt));
        }

        return result;
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> GetMessagesAsync(string userId, string conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null || !conversation.HasParticipant(userId))
            return Result.Failure<IReadOnlyList<MessageDto>>("Чат не знайдено.");

        IReadOnlyList<MessageDto> messages = conversation.Messages
            .Select(ToDto)
            .ToList();

        return Result.Success(messages);
    }

    public async Task<Result<MessageDto>> SendTextAsync(string userId, string conversationId, string text, CancellationToken cancellationToken = default)
    {
        var value = (text ?? string.Empty).Trim();
        if (value.Length == 0 || value.Length > MaxTextLength)
            return Result.Failure<MessageDto>($"Повідомлення має містити від 1 до {MaxTextLength} символів.");

        var conversation = await _conversations.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null || !conversation.HasParticipant(userId))
            return Result.Failure<MessageDto>("Чат не знайдено.");

        var message = new ChatMessage(userId, MessageKind.Text, value, null);
        conversation.AddMessage(message);
        await _conversations.UpdateAsync(conversation, cancellationToken);

        return Result.Success(ToDto(message));
    }

    public async Task<string> OpenConversationAsync(string clientId, string consultantId, CancellationToken cancellationToken = default)
    {
        var existing = await _conversations.GetByPairAsync(clientId, consultantId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var conversation = new Conversation(clientId, consultantId);
        await _conversations.AddAsync(conversation, cancellationToken);
        return conversation.Id;
    }

    public async Task PostSystemMessageAsync(string conversationId, string text, string? link, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
            return;

        var kind = link is null ? MessageKind.System : MessageKind.ConsultationLink;
        conversation.AddMessage(new ChatMessage(null, kind, text, link));
        await _conversations.UpdateAsync(conversation, cancellationToken);
    }

    private static MessageDto ToDto(ChatMessage message) =>
        new(message.Kind.ToString(), message.SenderId, message.Text, message.Link, message.CreatedAt);
}

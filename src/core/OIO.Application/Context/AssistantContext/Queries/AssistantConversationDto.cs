namespace OIO.Application.Context.AssistantContext.Queries;

public sealed record AssistantConversationDto(
    Guid Id,
    string RoleContext,
    string Title,
    DateTime CreatedAt,
    DateTime LastMessageAt);

public sealed record AssistantMessageDto(
    Guid Id,
    Guid ConversationId,
    string Sender,
    string Content,
    string? Citations,
    string? Metadata,
    DateTime CreatedAt);

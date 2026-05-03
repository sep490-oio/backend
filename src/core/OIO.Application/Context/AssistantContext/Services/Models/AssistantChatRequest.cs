namespace OIO.Application.Context.AssistantContext.Services.Models;

public sealed record AssistantChatRequest(
    AssistantRequestContext Context,
    string UserText,
    IReadOnlyList<ConversationHistoryMessage> History);

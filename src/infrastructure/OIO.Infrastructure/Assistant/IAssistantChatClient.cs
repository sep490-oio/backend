using Microsoft.Extensions.AI;

namespace OIO.Infrastructure.Assistant;

/// <summary>
/// Typed wrapper around <see cref="IChatClient"/> dedicated to the assistant.
/// Avoids DI conflict with the ProductDescription <see cref="IChatClient"/>
/// already registered in <c>AddAiSuggestion</c>. Internal — only consumed
/// within the Infrastructure layer (AssistantChatService).
/// </summary>
internal interface IAssistantChatClient
{
    Task<ChatResponse> GetResponseAsync(
        IList<ChatMessage> messages,
        ChatOptions options,
        CancellationToken cancellationToken);

    bool IsEnabled { get; }
}

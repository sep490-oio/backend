namespace OIO.Application.Context.AssistantContext.Services.Models;

/// <summary>
/// Request-scoped context propagated to retriever, tool executor, and chat service.
/// Authorization checks in <see cref="IAssistantToolExecutor"/> rely on UserId + Role.
/// </summary>
public sealed record AssistantRequestContext(
    Guid? UserId,
    string Role,
    string Locale,
    PageContext Page);

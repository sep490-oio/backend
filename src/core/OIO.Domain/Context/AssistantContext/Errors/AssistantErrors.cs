using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AssistantContext.Errors;

public static class AssistantErrors
{
    public static readonly Error Disabled = Error.Conflict(
        "Assistant.Disabled",
        "AI Assistant is disabled.");

    public static readonly Error ProviderUnavailable = Error.Unavailable(
        "Assistant.ProviderUnavailable",
        "AI Assistant provider is unavailable. Please try again later.");

    public static readonly Error RateLimited = Error.Conflict(
        "Assistant.RateLimited",
        "Too many requests. Please wait and try again.");

    public static readonly Error InvalidInput = Error.Conflict(
        "Assistant.InvalidInput",
        "The provided message is invalid.");

    public static readonly Error PromptInjectionDetected = Error.Conflict(
        "Assistant.PromptInjection",
        "The message contains content that the assistant cannot process.");

    public static readonly Error ConversationNotFound = Error.NotFound(
        "Assistant.ConversationNotFound",
        "Conversation was not found or you do not have access.");

    public static readonly Error ConversationAccessDenied = Error.Forbidden(
        "Assistant.ConversationAccessDenied",
        "You do not have access to this conversation.");
}

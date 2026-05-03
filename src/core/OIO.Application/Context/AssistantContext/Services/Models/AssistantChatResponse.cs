namespace OIO.Application.Context.AssistantContext.Services.Models;

public sealed record AssistantChatResponse(
    string Answer,
    IReadOnlyList<Citation> Citations,
    IReadOnlyList<SuggestedAction> SuggestedActions,
    double Confidence,
    bool NeedsHumanSupport,
    int? TokenUsage = null);

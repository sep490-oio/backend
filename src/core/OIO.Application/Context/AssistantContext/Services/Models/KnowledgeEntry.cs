namespace OIO.Application.Context.AssistantContext.Services.Models;

public sealed record KnowledgeEntry(
    string Id,
    string Title,
    IReadOnlyList<string> Tags,
    string Summary,
    string Content,
    string? SourceUrl);

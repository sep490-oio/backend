namespace OIO.Application.Context.AssistantContext.Services.Models;

public sealed record ToolRequest(
    string Name,
    IReadOnlyDictionary<string, string?> Arguments);

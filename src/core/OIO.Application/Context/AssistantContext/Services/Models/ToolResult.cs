namespace OIO.Application.Context.AssistantContext.Services.Models;

public sealed record ToolResult(
    string Name,
    bool Success,
    string Summary,
    string? ErrorCode = null);

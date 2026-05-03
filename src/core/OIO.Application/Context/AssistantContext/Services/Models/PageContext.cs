namespace OIO.Application.Context.AssistantContext.Services.Models;

public sealed record PageContext(
    string Route,
    string? EntityType = null,
    Guid? EntityId = null);

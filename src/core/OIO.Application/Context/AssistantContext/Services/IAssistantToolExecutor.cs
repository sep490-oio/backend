using OIO.Application.Context.AssistantContext.Services.Models;

namespace OIO.Application.Context.AssistantContext.Services;

public interface IAssistantToolExecutor
{
    Task<IReadOnlyList<ToolResult>> ExecuteAsync(
        IReadOnlyList<ToolRequest> requests,
        AssistantRequestContext context,
        CancellationToken ct);
}

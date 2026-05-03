using Microsoft.Extensions.Logging;
using OIO.Application.Context.AssistantContext.Services;
using OIO.Application.Context.AssistantContext.Services.Models;

namespace OIO.Infrastructure.Assistant;

/// <summary>
/// v1 read-only tool executor. The plan calls for role-aware tools that query
/// orders/auctions/wallets, but actually wiring those queries safely requires
/// careful authz against multiple aggregates (Order, Auction, Wallet, Dispute,
/// Shipment) — each with its own ownership rules. To avoid risk, v1 simply
/// returns a "tool not yet enabled" hint when the assistant requests data;
/// the system prompt steers the model to answer from KNOWLEDGE and to suggest
/// deep links instead of fabricating values.
/// v2 will add concrete tools after a security review of each query.
/// </summary>
internal sealed class AssistantToolExecutor : IAssistantToolExecutor
{
    private readonly ILogger<AssistantToolExecutor> _logger;

    public AssistantToolExecutor(ILogger<AssistantToolExecutor> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<ToolResult>> ExecuteAsync(
        IReadOnlyList<ToolRequest> requests,
        AssistantRequestContext context,
        CancellationToken ct)
    {
        if (requests.Count == 0)
            return Task.FromResult<IReadOnlyList<ToolResult>>([]);

        var results = new List<ToolResult>(requests.Count);
        foreach (var request in requests)
        {
            _logger.LogDebug(
                "Assistant tool requested but disabled in v1: {Tool} role={Role}",
                request.Name, context.Role);
            results.Add(new ToolResult(
                request.Name,
                Success: false,
                Summary: "This tool is not enabled in this version. " +
                        "Provide the user with a recommended page link instead.",
                ErrorCode: "tool_not_available"));
        }
        return Task.FromResult<IReadOnlyList<ToolResult>>(results);
    }
}

using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.ModerationContext.Queries.GetReports;

public record GetReportsQueryFilters : PagedParameters
{
    public string? Status { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
}
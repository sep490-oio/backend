using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.UserContext.Queries.GetPermissions;

public record PermissionFilterParameters : PagedParameters
{
    public string? Search { get; init; }
}
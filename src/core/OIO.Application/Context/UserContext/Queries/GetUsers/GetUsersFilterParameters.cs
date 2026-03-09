using OIO.Application.Abstractions.Commons;

namespace OIO.Application.Context.UserContext.Queries.GetUsers;

public record GetUsersFilterParameters : PagedParameters, ISortByParameter
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? Role { get; init; }
    public string? SortBy { get; init; }
}
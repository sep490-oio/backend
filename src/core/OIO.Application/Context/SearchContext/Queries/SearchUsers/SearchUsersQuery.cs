using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.SearchContext.Queries.SearchUsers;

public record SearchUsersQuery(
    string Query,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool SortDescending = true,
    string? Role = null) : IRequest<PagedList<UserDto>>;

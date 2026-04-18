using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.SearchContext.Queries.SearchUsers;

public class SearchUsersQueryHandler(IElasticsearchService searchService)
    : IRequestHandler<SearchUsersQuery, PagedList<UserDto>>
{
    public async Task<PagedList<UserDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var filters = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(request.Role)) filters["roles.keyword"] = request.Role;

        var searchResult = await searchService.SearchAsync<UserSearchDocument>(
            request.Query,
            [searchService.UsersIndex],
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            filters,
            cancellationToken: cancellationToken);

        var mappedResults = searchResult.Results.Select(doc => new UserDto(
            Id: Guid.Parse(doc.Id),
            UserName: doc.UserName,
            Email: doc.Email,
            EmailConfirmed: true,
            PhoneNumber: doc.PhoneNumber,
            CountryCode: null,
            PhoneNumberConfirmed: true,
            TwoFactorEnabled: false,
            TwoFactorProvider: "None",
            Status: doc.Status,
            CreatedAt: doc.CreatedAt,
            Profile: new UserProfileDto(null, null, doc.UserName, doc.FullName, null, null, null)
        )).ToList();

        return new PagedList<UserDto>(
            mappedResults,
            (int)searchResult.Total,
            searchResult.Page,
            searchResult.PageSize);
    }
}

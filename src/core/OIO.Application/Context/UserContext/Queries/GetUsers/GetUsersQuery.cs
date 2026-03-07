using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Queries.Filters;
using OIO.Application.Extensions;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetUsers;

public sealed record GetUsersQuery(
    UserFilterParameters Parameters) : IQuery<PagedList<UserListItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetUsersQuery.Check()
            .WithOwnerName("GetUsers")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(UserStatus.All.Select(y => y.Id)))
            .Field(Parameters.Role)
            .WhenHasValue(x => x.InSet(App.Roles.Catalogs.All));
    }
}

internal sealed class GetUsersQueryHandler
    : IQueryHandler<GetUsersQuery, PagedList<UserListItemDto>>
{
    private readonly IDbContext _dbContext;
    private readonly SortMappingProvider _sortMappingProvider;

    public GetUsersQueryHandler(IDbContext dbContext, SortMappingProvider sortMappingProvider)
    {
        _dbContext = dbContext;
        _sortMappingProvider = sortMappingProvider;
    }

    public async Task<Result<PagedList<UserListItemDto>, Error>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        
        if (!_sortMappingProvider.ValidateMappings<UserListItemDto, User>(parameters.SortBy))
        {
            return Error.Validation("SortBy", "SortBy.Invalid",
                $"The provided sort parameter isn't valid: '{parameters.SortBy}'");
        }

        var query = _dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(x => x.Profile)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var searchTerm = parameters.Search.ToLower();
            query = query.Where(u =>
                u.UserName.Value.ToLower().Contains(searchTerm) ||
                u.Email.Value.ToLower().Contains(searchTerm));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = UserStatus.FromId(parameters.Status);
            query = query.Where(u => u.Status == status);
        }

        // Role filter
        if (!string.IsNullOrWhiteSpace(parameters.Role))
        {
            query = query.Where(u =>
                u.Roles.Any(r => r.Role.RoleName == parameters.Role.ToLower()));
        }

        // Sorting
        //query.ApplySort(parameters, _sortMappingProvider.GetMappings<UserListItemDto, User>());

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Page(parameters)
            .Select(u => new UserListItemDto(
                u.Id.Value,
                u.UserName.Value,
                u.Email.Value,
                u.Profile != null ? u.Profile.FirstName != null ?  u.Profile.FirstName .Value : null : null,
                u.Profile != null ? u.Profile.LastName != null ?  u.Profile.LastName .Value : null : null,
                u.Status.Id,
                u.EmailConfirmedAt != null,
                u.Roles.Select(r => r.Role.RoleName).ToList(),
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedList<UserListItemDto>.ToPagedList(
            items, 
            totalCount,
            parameters);
    }
}
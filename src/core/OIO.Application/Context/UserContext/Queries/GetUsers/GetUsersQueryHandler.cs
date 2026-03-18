using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetUsers;

internal sealed class GetUsersQueryHandler
    : IQueryHandler<GetUsersQuery, PagedList<UserListItemDto>>
{
    private readonly IDbContext _dbContext;

    public GetUsersQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<UserListItemDto>, Error>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = _dbContext.Set<User>()
            .AsNoTracking();

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
            var status = UserStatus.FromId(parameters.Status).Value;
            query = query.Where(u => u.Status == status);
        }

        // Role filter
        if (!string.IsNullOrWhiteSpace(parameters.Role))
        {
            query = query.Where(u =>
                u.Roles.Any(r => r.Role.Name == parameters.Role.ToLower()));
        }

        query = query.ApplySort(parameters, UserMappings.UserListItemDtoSortMapping);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Select(u => new UserListItemDto(
                u.Id.Value,
                u.UserName.Value,
                u.Email.Value,
                u.Profile != null ? u.Profile.Name.FirstName != null ?  u.Profile.Name.FirstName : null : null,
                u.Profile != null ? u.Profile.Name.LastName != null ?  u.Profile.Name.LastName : null : null,
                u.Status.Id,
                u.EmailConfirmedAt != null,
                u.Roles.Select(r => r.Role.Name).ToList(),
                u.CreatedAt))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return items;
    }
}
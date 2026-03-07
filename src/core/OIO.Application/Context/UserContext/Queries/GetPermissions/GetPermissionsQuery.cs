using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Queries.Filters;
using OIO.Application.Extensions;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetPermissions;

public sealed record GetPermissionsQuery(
    PermissionFilterParameters Parameters) : IQuery<PagedList<PermissionDto>>;
    
internal sealed class GetPermissionsQueryHandler
    : IQueryHandler<GetPermissionsQuery, PagedList<PermissionDto>>
{
    private readonly IDbContext _dbContext;

    public GetPermissionsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<PermissionDto>, Error>> Handle(
        GetPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = _dbContext.Set<Permission>()
            .AsNoTracking()
            .AsQueryable();

        // Search by permission code
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var searchTerm = parameters.Search.ToLower();
            query = query.Where(p => p.PermissionCode.ToLower().Contains(searchTerm));
        }

        query = query.OrderBy(p => p.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Page(parameters)
            .Select(p => new PermissionDto(p.Id.Value, p.PermissionCode))
            .ToListAsync(cancellationToken);

        return PagedList<PermissionDto>.ToPagedList(
            items, totalCount, parameters);
    }
}
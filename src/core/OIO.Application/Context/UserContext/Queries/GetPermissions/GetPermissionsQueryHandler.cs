using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetPermissions;

internal sealed class GetPermissionsQueryHandler
    : IQueryHandler<GetPermissionsQuery, PagedList<string>>
{
    private readonly IDbContext _dbContext;

    public GetPermissionsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<string>, Error>> Handle(
        GetPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = _dbContext.Set<Permission>()
            .AsNoTracking();

        // Search by permission code
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var searchTerm = parameters.Search.ToLower();
            query = query.Where(p => p.Code.ToLower().Contains(searchTerm));
        }
        

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Code)
            .Select(p => p.Code)
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return items;
    }
}
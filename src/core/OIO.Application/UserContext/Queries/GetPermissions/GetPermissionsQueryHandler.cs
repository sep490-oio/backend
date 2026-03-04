using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Extensions;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Queries.GetPermissions;

internal sealed class GetPermissionsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetPermissionsQuery, PagedList<PermissionDto>>
{
    public async Task<Result<PagedList<PermissionDto>, Error>> Handle(
        GetPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = await dbContext.Set<Permission>()
            .OrderBy(r => r.Id)
            .Page(request.PagedParameters)
            .Select(r => new PermissionDto(
                Id: r.Id,
                PermissionCode: r.PermissionCode))
            .ToListAsync(cancellationToken);

        var totalCount = await dbContext.Set<Permission>()
            .CountAsync(cancellationToken);

        return PagedList<PermissionDto>.ToPagedList(
            permissions,
            totalCount,
            request.PagedParameters);
    }
}
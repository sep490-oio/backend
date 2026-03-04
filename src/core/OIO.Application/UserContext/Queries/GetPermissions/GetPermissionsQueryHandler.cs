using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Queries.GetPermissions;

internal sealed class GetPermissionsQueryHandler(IDbContext dbContext) : IQueryHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    public async Task<Result<IReadOnlyList<PermissionDto>, Error>> Handle(
        GetPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.Set<Permission>()
            .OrderBy(r => r.Id)
            .Select(r => new PermissionDto(
                Id: r.Id,
                PermissionCode: r.PermissionCode))
            .ToListAsync(cancellationToken);

        return roles; 
    }
}
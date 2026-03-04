using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Queries.GetRoles;

internal sealed class GetRolesQueryHandler(IDbContext dbContext) : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<Result<IReadOnlyList<RoleDto>, Error>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.Set<Role>()
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto(
                Id: r.Id,
                RoleName: r.RoleName))
            .ToListAsync(cancellationToken);

        return roles; 
    }
}
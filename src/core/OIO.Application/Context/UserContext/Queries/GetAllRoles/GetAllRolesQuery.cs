using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Roles;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetAllRoles;

public sealed record GetAllRolesQuery : IQuery<IReadOnlyList<RoleDto>>;

internal sealed class GetAllRolesQueryHandler
    : IQueryHandler<GetAllRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IDbContext _dbContext;

    public GetAllRolesQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<RoleDto>, Error>> Handle(
        GetAllRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Set<Role>()
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Name,
                r.RolePermissions
                    .Where(rp => rp.IsActive)
                    .Select(rp => rp.Permission.Code)
                    .ToList()))
            .ToListAsync(cancellationToken);

        return roles;
    }
}
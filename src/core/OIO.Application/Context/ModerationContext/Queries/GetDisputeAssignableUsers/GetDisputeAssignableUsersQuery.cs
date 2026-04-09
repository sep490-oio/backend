using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetDisputeAssignableUsers;

public sealed record GetDisputeAssignableUsersQuery(Guid DisputeId) : IQuery<List<DisputeAssignableUserDto>>;

internal sealed class GetDisputeAssignableUsersQueryHandler(
    IDbContext dbContext)
    : IQueryHandler<GetDisputeAssignableUsersQuery, List<DisputeAssignableUserDto>>
{
    private static readonly HashSet<string> StaffRoles =
    [
        App.Roles.Catalogs.Admin,
        App.Roles.Catalogs.WarehouseStaff,
        App.Roles.Catalogs.Inspector
    ];

    public async Task<Result<List<DisputeAssignableUserDto>, Error>> Handle(
        GetDisputeAssignableUsersQuery request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);

        var dispute = await dbContext.Set<Dispute>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{request.DisputeId}' not found.");

        var caseDomain = dispute.CaseDomain;

        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.Roles)
            .Where(u => u.Roles.Any(r => StaffRoles.Contains(r.RoleName)))
            .ToListAsync(cancellationToken);

        var result = users.Select(u =>
        {
            var primaryRole = u.Roles
                .Where(r => StaffRoles.Contains(r.RoleName))
                .OrderByDescending(r => r.RoleName == App.Roles.Catalogs.Admin ? 2 : 1)
                .First();

            var domains = MapDomainCapabilities(primaryRole.RoleName);

            return new DisputeAssignableUserDto(
                u.Id.Value,
                u.UserName.Value,
                primaryRole.RoleName,
                domains);
        }).ToList();

        // If there's a known case domain, prioritize users with that capability
        // but still return all staff users
        if (caseDomain is not null)
        {
            result = result
                .OrderByDescending(u => u.DomainCapabilities.Contains(caseDomain))
                .ThenBy(u => u.DisplayName)
                .ToList();
        }
        else
        {
            result = result.OrderBy(u => u.DisplayName).ToList();
        }

        return result;
    }

    private static List<string> MapDomainCapabilities(string roleName)
    {
        if (roleName == App.Roles.Catalogs.Admin)
            return ["auction_settlement", "item_condition", "payment", "shipping"];

        if (roleName == App.Roles.Catalogs.WarehouseStaff)
            return ["shipping"];

        if (roleName == App.Roles.Catalogs.Inspector)
            return ["item_condition"];

        return [];
    }
}

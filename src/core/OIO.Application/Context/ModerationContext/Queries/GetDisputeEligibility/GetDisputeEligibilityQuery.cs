using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetDisputeEligibility;

/// <summary>
/// Read-side query: resolves the caller's role + matrix for a single (target, entity)
/// pair. Advisory only — BE submit-time invariant in <c>Dispute.CreateCase</c> remains
/// the sole authority. See <see cref="DisputeEligibilityDto"/>.
/// </summary>
public sealed record GetDisputeEligibilityQuery(
    string TargetType,
    Guid EntityId) : IQuery<DisputeEligibilityDto>;

internal sealed class GetDisputeEligibilityQueryHandler(
    IDisputeEligibilityService eligibilityService,
    ICurrentUser currentUser,
    IDbContext dbContext)
    : IQueryHandler<GetDisputeEligibilityQuery, DisputeEligibilityDto>
{
    private static readonly IReadOnlyList<string> EmptyDomains = Array.Empty<string>();
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyCaseTypes
        = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

    public async Task<Result<DisputeEligibilityDto, Error>> Handle(
        GetDisputeEligibilityQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId.Value;

        var role = await eligibilityService.ResolveRoleAsync(
            userId,
            request.TargetType,
            request.EntityId,
            cancellationToken);

        if (role is null)
        {
            // Either the entity is missing or the caller isn't a recognised participant.
            return new DisputeEligibilityDto(
                CanReport: false,
                Role: null,
                AllowedDomains: EmptyDomains,
                AllowedCaseTypes: EmptyCaseTypes,
                Reason: "OutOfScope");
        }

        var timingAllowed = await eligibilityService.IsTimingAllowedAsync(
            request.TargetType,
            request.EntityId,
            cancellationToken);

        if (!timingAllowed)
        {
            // Today only auctions enforce a timing gate. Distinguish "still active"
            // (auction.Status == active) from "pre-bidding" (draft/pending/approved/scheduled)
            // by reading the actual status — avoids conflating role with timing reason.
            var reason = "OutOfScope";
            if (request.TargetType == DisputeEligibilityRule.TargetAuction)
            {
                var status = await dbContext.Set<Auction>()
                    .AsNoTracking()
                    .Where(a => a.Id == AuctionId.From(request.EntityId))
                    .Select(a => a.Status.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                reason = status == AuctionStatus.Active.Id
                    ? "AuctionStillActive"
                    : "AuctionPreBidding";
            }

            return new DisputeEligibilityDto(
                CanReport: false,
                Role: role,
                AllowedDomains: EmptyDomains,
                AllowedCaseTypes: EmptyCaseTypes,
                Reason: reason);
        }

        var allowedDomains = DisputeEligibilityRule.AllowedDomainsFor(role, request.TargetType);
        if (allowedDomains.Count == 0)
        {
            return new DisputeEligibilityDto(
                CanReport: false,
                Role: role,
                AllowedDomains: EmptyDomains,
                AllowedCaseTypes: EmptyCaseTypes,
                Reason: "NotParticipant");
        }

        var allowedCaseTypes = new Dictionary<string, IReadOnlyList<string>>(
            allowedDomains.Count,
            StringComparer.Ordinal);
        foreach (var domain in allowedDomains)
        {
            allowedCaseTypes[domain] = DisputeEligibilityRule.AllowedCaseTypesFor(
                role,
                request.TargetType,
                domain);
        }

        return new DisputeEligibilityDto(
            CanReport: true,
            Role: role,
            AllowedDomains: allowedDomains,
            AllowedCaseTypes: allowedCaseTypes,
            Reason: null);
    }
}

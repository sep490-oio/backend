using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Services;

public sealed class DisputeAccessService(
    IDbContext dbContext,
    ICurrentUser currentUser,
    IClock clock)
{
    public bool IsAdmin => currentUser.IsInRole(App.Roles.Catalogs.Admin);
    public bool CanViewInternalMessages => IsAdmin;
    public UserId CurrentUserId => currentUser.UserId;

    public async Task<Result<Dispute, Error>> GetAccessibleDisputeAsync(
        DisputeId disputeId,
        Func<IQueryable<Dispute>, IQueryable<Dispute>>? queryBuilder = null,
        CancellationToken cancellationToken = default)
    {
        var dispute = await dbContext.GetByIdAsync<Dispute, DisputeId>(
            disputeId,
            queryBuilder,
            cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        return CanAccess(dispute)
            ? dispute
            : Error.Forbidden("Dispute.Forbidden", "You are not allowed to access this dispute.");
    }

    public UnitResult<Error> EnsureInternalMessageAllowed(bool isInternal)
    {
        if (!isInternal || IsAdmin)
            return UnitResult.Success<Error>();

        return Error.Forbidden("Dispute.InternalMessageForbidden", "Only admins can send internal messages.");
    }

    public bool CanAccess(Dispute dispute) =>
        IsAdmin ||
        dispute.ComplainantId == currentUser.UserId ||
        dispute.RespondentId == currentUser.UserId;

    public async Task<DisputeParticipantState> EnsureParticipantStateAsync(
        DisputeId disputeId,
        CancellationToken cancellationToken = default)
    {
        var state = await dbContext.Set<DisputeParticipantState>()
            .FirstOrDefaultAsync(
                x => x.DisputeId == disputeId && x.UserId == currentUser.UserId,
                cancellationToken);

        if (state is not null)
        {
            state.Touch(clock.UtcNow);
            return state;
        }

        state = DisputeParticipantState.Create(disputeId, currentUser.UserId, clock.UtcNow);
        dbContext.Insert(state);
        return state;
    }
}

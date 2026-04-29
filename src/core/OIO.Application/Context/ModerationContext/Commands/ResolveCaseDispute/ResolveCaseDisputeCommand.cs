using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.ResolveCaseDispute;

public sealed record ResolveCaseActionSetRequest(
    string? EscrowAction,
    string? RefundAction,
    decimal? RefundAmount,
    string? ShipmentAction,
    string? ItemAction,
    string? AuctionAction,
    string? PenaltyAction);

public sealed record ResolveCaseDisputeCommand(
    Guid DisputeId,
    string Outcome,
    string Reason,
    ResolveCaseActionSetRequest? ActionSet = null) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ResolveCaseDisputeCommand.Check()
            .WithOwnerName("ResolveCaseDispute")
            .Field(DisputeId).NotEmptyGuid()
            .Field(Outcome).NotWhiteSpace()
            .Field(Reason).NotWhiteSpace();
    }
}

internal sealed class ResolveCaseDisputeCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IDisputeResolutionService resolutionService)
    : ICommandHandler<ResolveCaseDisputeCommand>
{
    public async Task<UnitResult<Error>> Handle(
        ResolveCaseDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var disputeId = DisputeId.From(request.DisputeId);

        if (request.ActionSet is { } actionSet)
        {
            if (actionSet.RefundAction == "partial_refund" && actionSet.RefundAmount is null or <= 0)
                return Error.Validation("ActionSet.RefundAmount", "ResolveCaseDispute.PartialRefundRequiresAmount",
                    "RefundAmount must be greater than 0 when RefundAction is 'partial_refund'.");

            if (actionSet.EscrowAction == "partial_refund" && actionSet.RefundAmount is null or <= 0)
                return Error.Validation("ActionSet.RefundAmount", "ResolveCaseDispute.EscrowPartialRefundRequiresAmount",
                    "RefundAmount must be greater than 0 when EscrowAction is 'partial_refund'.");

            if (actionSet.EscrowAction == "release_to_seller"
                && actionSet.RefundAction is "full_refund" or "partial_refund")
                return Error.Validation("ActionSet", "ResolveCaseDispute.ConflictingActions",
                    "Cannot release to seller and refund buyer simultaneously.");

            var escrowRefundsBuyer = actionSet.EscrowAction is "refund_buyer" or "partial_refund";
            var refundActionRefundsBuyer = actionSet.RefundAction is "full_refund" or "partial_refund";
            if (escrowRefundsBuyer && refundActionRefundsBuyer)
                return Error.Validation("ActionSet", "ResolveCaseDispute.ConflictingRefundActions",
                    "Cannot request buyer refund through both EscrowAction and RefundAction.");
        }

        var dispute = await dbContext.GetByIdAsync<Dispute, DisputeId>(
            id: disputeId,
            queryBuilder: q => q.Include(d => d.StatusHistory),
            cancellationToken: cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        string? actionSetJson = request.ActionSet is not null
            ? JsonSerializer.Serialize(request.ActionSet)
            : null;

        var result = dispute.ResolveCase(
            request.Outcome,
            request.Reason,
            actionSetJson,
            currentUser.UserId.Value,
            nowUtc);

        if (result.IsFailure) return result.Error;

        var applyResult = await resolutionService.ApplyResolutionAsync(dispute, cancellationToken);
        if (applyResult.IsFailure) return applyResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}

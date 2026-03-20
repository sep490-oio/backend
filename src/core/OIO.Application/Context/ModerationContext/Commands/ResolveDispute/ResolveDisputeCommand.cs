using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.Events;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.ResolveDispute;

public sealed record ResolveDisputeCommand(
    Guid DisputeId,
    string ResolutionType,
    string? Notes = null,
    decimal? Amount = null) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ResolveDisputeCommand.Check()
            .WithOwnerName("ResolveDispute")
            .Field(DisputeId).NotEmptyGuid()
            .Field(ResolutionType).NotWhiteSpace();
    }
}

internal sealed class ResolveDisputeCommandHandler : ICommandHandler<ResolveDisputeCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly EscrowSettlementService _settlementService;
    private readonly IPublisher _publisher;

    public ResolveDisputeCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        EscrowSettlementService settlementService,
        IPublisher publisher)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _settlementService = settlementService;
        _publisher = publisher;
    }

    public async Task<UnitResult<Error>> Handle(
        ResolveDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var disputeId = DisputeId.From(request.DisputeId);
        var adminId = _currentUser.UserId;

        var dispute = await _dbContext.GetByIdAsync<Dispute, DisputeId>(
            id: disputeId,
            queryBuilder: q => q.Include(d => d.StatusHistory),
            cancellationToken: cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        var resolutionType = ResolutionType.FromId(request.ResolutionType);
        if (resolutionType.HasNoValue)
            return Error.Validation("ResolutionType", "Dispute.InvalidResolutionType",
                $"Invalid resolution type: {request.ResolutionType}");

        var result = dispute.Resolve(resolutionType.Value, adminId, nowUtc, request.Notes, request.Amount);
        if (result.IsFailure) return result.Error;

        var hasOrder = dispute.OrderId != OrderId.From(Guid.Empty);
        Order? order = null;
        if (hasOrder)
        {
            order = await _dbContext.Set<Order>()
                .Include(x => x.Return)
                .Include(x => x.Escrows)
                .FirstOrDefaultAsync(x => x.Id == dispute.OrderId, cancellationToken);
        }

        if (order is not null)
        {
            var markDisputedResult = order.MarkAsDisputed(nowUtc);
            if (markDisputedResult.IsFailure && order.Status.Id != "disputed")
                return markDisputedResult.Error;

            if (resolutionType.Value == ResolutionType.FavorSeller ||
                resolutionType.Value == ResolutionType.MutualAgreement)
            {
                var releaseResult = await _settlementService.ReleaseToSellerAsync(
                    order,
                    request.Notes ?? resolutionType.Value.Id,
                    adminId,
                    cancellationToken);

                if (releaseResult.IsFailure)
                    return releaseResult.Error;
            }
            else if (resolutionType.Value == ResolutionType.FavorBuyer ||
                     resolutionType.Value == ResolutionType.RefundFull)
            {
                var refundResult = await _settlementService.RefundBuyerAsync(
                    order,
                    partialAmount: null,
                    reason: request.Notes ?? resolutionType.Value.Id,
                    actorId: adminId,
                    cancellationToken: cancellationToken);

                if (refundResult.IsFailure)
                    return refundResult.Error;
            }
            else if (resolutionType.Value == ResolutionType.RefundPartial)
            {
                var refundResult = await _settlementService.RefundBuyerAsync(
                    order,
                    partialAmount: request.Amount,
                    reason: request.Notes ?? resolutionType.Value.Id,
                    actorId: adminId,
                    cancellationToken: cancellationToken);

                if (refundResult.IsFailure)
                    return refundResult.Error;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new DisputeChangedEvent(dispute.Id, nowUtc),
            cancellationToken);

        return UnitResult.Success<Error>();
    }
}

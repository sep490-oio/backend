using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.TriggerAuctionEmergency;

public sealed record TriggerAuctionEmergencyCommand(
    Guid AuctionId,
    string TriggerSource,
    string Reason,
    object Payload) : ICommand<AuctionEmergencyDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return TriggerAuctionEmergencyCommand.Check()
            .WithOwnerName("TriggerAuctionEmergency")
            .Field(AuctionId).NotEmptyGuid()
            .Field(TriggerSource).NotWhiteSpace()
            .Field(Reason).NotWhiteSpace();
    }
}

internal sealed class TriggerAuctionEmergencyCommandHandler
    : ICommandHandler<TriggerAuctionEmergencyCommand, AuctionEmergencyDto>
{
    private readonly ISender _sender;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly EscrowSettlementService _settlementService;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly ModerationAuditService _auditService;
    private readonly ILogger<TriggerAuctionEmergencyCommandHandler> _logger;

    public TriggerAuctionEmergencyCommandHandler(
        ISender sender,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        EscrowSettlementService settlementService,
        IRuntimeSettings runtimeSettings,
        ModerationAuditService auditService,
        ILogger<TriggerAuctionEmergencyCommandHandler> logger)
    {
        _sender = sender;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _settlementService = settlementService;
        _runtimeSettings = runtimeSettings;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Result<AuctionEmergencyDto, Error>> Handle(
        TriggerAuctionEmergencyCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var order = await _dbContext.Set<Order>()
            .Include(x => x.Escrows)
            .FirstOrDefaultAsync(x => x.AuctionId == auctionId, cancellationToken);

        var outboundShipments = order is null
            ? []
            : await _dbContext.Set<OutboundShipment>()
                .Where(x => x.OrderId == order.Id)
                .ToListAsync(cancellationToken);

        if (outboundShipments.Any(x =>
                x.Status == OutboundShipmentStatus.PickedUp ||
                x.Status == OutboundShipmentStatus.InTransit ||
                x.Status == OutboundShipmentStatus.Delivered ||
                x.Status == OutboundShipmentStatus.Returning ||
                x.Status == OutboundShipmentStatus.Returned))
        {
            return AuctionErrors.Auction.EmergencyBlockedByShipment;
        }

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => a.Emergencies)
                .Include(a => a.Item)
                .Include(a => a.Bids)
                .Include(a => a.AutoBids)
                .Include(a => a.WinnerOffers),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var emergencyResult = auction.TriggerEmergency(
            _currentUser.UserId,
            request.TriggerSource,
            request.Reason,
            JsonSerializer.Serialize(request.Payload),
            _clock.UtcNow);

        if (emergencyResult.IsFailure)
            return emergencyResult.Error;

        var terminateResult = auction.Terminate(request.Reason, _clock.UtcNow);
        if (terminateResult.IsFailure)
            return terminateResult.Error;

        _auditService.Log(
            action: "auction_emergency_triggered",
            entityType: "Auction",
            entityId: auction.Id.Value,
            newData: new
            {
                triggerSource = request.TriggerSource,
                reason = request.Reason,
                status = auction.Status.Id
            });

        var removeItemResult = auction.Item.Remove(_clock.UtcNow);
        if (removeItemResult.IsFailure)
        {
            _logger.LogWarning("Failed to remove item {ItemId} during emergency: {Error}", auction.Item.Id.Value, removeItemResult.Error.Message);
        }

        if (order is not null)
        {
            if (order.Status == OrderStatus.PendingPayment)
            {
                var cancelResult = order.Cancel($"Emergency termination: {request.Reason}", _clock.UtcNow);
                if (cancelResult.IsFailure)
                    return cancelResult.Error;
            }
            else if (order.Escrows.Any(x => x.Status == EscrowStatus.Holding))
            {
                var refundResult = await _settlementService.RefundBuyerAsync(
                    order,
                    partialAmount: null,
                    reason: $"Emergency termination: {request.Reason}",
                    actorId: null,
                    cancellationToken: cancellationToken);

                if (refundResult.IsFailure)
                    return refundResult.Error;
            }

            foreach (var shipment in outboundShipments.Where(x =>
                         x.Status == OutboundShipmentStatus.Pending ||
                         x.Status == OutboundShipmentStatus.Booked))
            {
                var cancelResult = shipment.Cancel(
                    $"Emergency termination: {request.Reason}",
                    _clock.UtcNow);

                if (cancelResult.IsFailure)
                    return cancelResult.Error;
            }
        }

        var sellerRiskFlag = UserRiskFlag.Create(
            userId: auction.Item.SellerId,
            flagType: "auction_emergency",
            reason: request.Reason,
            severity: RiskFlagSeverity.High,
            createdBy: null,
            nowUtc: _clock.UtcNow);

        _dbContext.Insert(sellerRiskFlag);

        var autoSuspend = _runtimeSettings.Ops.AutoSuspendOnEmergency;

        if (autoSuspend)
        {
            var seller = await _dbContext.Set<User>()
                .FirstOrDefaultAsync(x => x.Id == auction.Item.SellerId, cancellationToken);

            if (seller is not null && seller.Status == UserStatus.Active)
            {
                var suspendResult = seller.ChangeStatus(UserStatus.Suspended, _clock.UtcNow);
                if (suspendResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to auto suspend seller {SellerId} after auction emergency {AuctionId}: {Error}",
                        seller.Id.Value,
                        auction.Id.Value,
                        suspendResult.Error.Message);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await AuctionDepositReleaseDispatch.ReturnHeldDepositsAsync(
            _dbContext,
            _sender,
            _logger,
            request.AuctionId,
            $"Auction emergency triggered: {request.Reason}",
            cancellationToken);

        return emergencyResult.Value.ToDto();
    }
}




using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Services;

internal sealed class OrderReceiptService : IOrderReceiptService
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly EscrowSettlementService _escrowSettlementService;

    public OrderReceiptService(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        EscrowSettlementService escrowSettlementService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _escrowSettlementService = escrowSettlementService;
    }

    public async Task<UnitResult<Error>> ConfirmAsync(
        Guid orderId,
        bool systemInvoked,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var orderIdValue = OrderId.From(orderId);
        var order = await _dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == orderIdValue, cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(orderIdValue);

        if (!systemInvoked && order.BuyerId != _currentUser.UserId)
            return Error.Forbidden("Order.Forbidden", "Only the buyer can confirm receipt for this order.");

        UserId? actorId = systemInvoked ? null : _currentUser.UserId;

        // Side-effect: when a SellerDirectShipment exists, advance it through
        // delivered → accepted → completed in lockstep with the escrow release.
        var shipment = await _dbContext.Set<SellerDirectShipment>()
            .FirstOrDefaultAsync(s => s.OrderId == orderIdValue, cancellationToken);

        if (shipment is not null)
        {
            var acceptResult = shipment.MarkAccepted(nowUtc);
            if (acceptResult.IsFailure)
                return acceptResult.Error;
        }

        var releaseResult = await _escrowSettlementService.ReleaseToSellerAsync(
            order,
            systemInvoked
                ? "Auto-confirmed after return-decision window expired"
                : "Buyer confirmed receipt",
            actorId,
            cancellationToken);

        if (releaseResult.IsFailure)
            return releaseResult.Error;

        if (shipment is not null)
        {
            var completeResult = shipment.MarkCompleted(nowUtc);
            if (completeResult.IsFailure)
                return completeResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}

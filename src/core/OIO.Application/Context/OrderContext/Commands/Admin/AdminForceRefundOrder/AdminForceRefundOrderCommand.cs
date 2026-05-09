using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.Admin.AdminForceRefundOrder;

public sealed record AdminForceRefundOrderCommand(Guid OrderId, string Reason) : ICommand;

internal sealed class AdminForceRefundOrderCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    EscrowSettlementService escrowSettlement,
    IClock clock,
    ILogger<AdminForceRefundOrderCommandHandler> logger)
    : ICommandHandler<AdminForceRefundOrderCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminForceRefundOrderCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;

        var order = await dbContext.GetByIdAsync<Order, OrderId>(
            OrderId.From(request.OrderId),
            queryBuilder: q => q
                .Include(o => o.Escrows)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (order is null)
            return Error.NotFound("Order.NotFound", "Order not found.");

        var refundStatusResult = order.AdminForceRefund(request.Reason, nowUtc);
        if (refundStatusResult.IsFailure)
            return refundStatusResult.Error;

        // Refund buyer via EscrowSettlementService
        var refundResult = await escrowSettlement.RefundBuyerAsync(
            order,
            partialAmount: null, // full refund
            reason: $"[ADMIN] Force refund: {request.Reason}",
            actorId: UserId.From(Guid.Empty), // System actor
            cancellationToken);

        if (refundResult.IsFailure)
        {
            logger.LogWarning(
                "Escrow refund failed during admin force-refund of order {OrderId}: {Error}",
                request.OrderId, refundResult.Error.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Admin force-refunded order {OrderId} ({OrderNumber}). Reason: {Reason}",
            request.OrderId, order.OrderNumber.Value, request.Reason);

        return UnitResult.Success<Error>();
    }
}

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

namespace OIO.Application.Context.OrderContext.Commands.Admin.AdminForceCancelOrder;

public sealed record AdminForceCancelOrderCommand(Guid OrderId, string Reason) : ICommand;

internal sealed class AdminForceCancelOrderCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    EscrowSettlementService escrowSettlement,
    IClock clock,
    ILogger<AdminForceCancelOrderCommandHandler> logger)
    : ICommandHandler<AdminForceCancelOrderCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminForceCancelOrderCommand request,
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

        var cancelResult = order.AdminForceCancel(request.Reason, nowUtc);
        if (cancelResult.IsFailure)
            return cancelResult.Error;

        // Refund buyer via EscrowSettlementService (handles escrow release + wallet credit)
        var refundResult = await escrowSettlement.RefundBuyerAsync(
            order,
            partialAmount: null, // full refund
            reason: $"[ADMIN] Force cancel: {request.Reason}",
            actorId: UserId.From(Guid.Empty), // System actor
            cancellationToken);

        if (refundResult.IsFailure)
        {
            // Log but don't fail the cancel — the order state change is more important
            logger.LogWarning(
                "Escrow refund failed during admin force-cancel of order {OrderId}: {Error}",
                request.OrderId, refundResult.Error.Message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Admin force-cancelled order {OrderId} ({OrderNumber}). Reason: {Reason}",
            request.OrderId, order.OrderNumber.Value, request.Reason);

        return UnitResult.Success<Error>();
    }
}

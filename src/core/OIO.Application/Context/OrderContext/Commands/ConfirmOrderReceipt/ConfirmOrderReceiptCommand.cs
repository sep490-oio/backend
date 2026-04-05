using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.ConfirmOrderReceipt;

public sealed record ConfirmOrderReceiptCommand(Guid OrderId) : ICommand;

internal sealed class ConfirmOrderReceiptCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    EscrowSettlementService escrowSettlementService)
    : ICommandHandler<ConfirmOrderReceiptCommand>
{
    public async Task<UnitResult<Error>> Handle(
        ConfirmOrderReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(orderId);

        // Only the buyer can confirm receipt
        if (order.BuyerId != currentUser.UserId)
            return Error.Forbidden("Order.Forbidden", "Only the buyer can confirm receipt for this order.");

        // Release escrow to seller and complete the order atomically
        var result = await escrowSettlementService.ReleaseToSellerAsync(
            order,
            "Buyer confirmed receipt",
            currentUser.UserId,
            cancellationToken);

        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}

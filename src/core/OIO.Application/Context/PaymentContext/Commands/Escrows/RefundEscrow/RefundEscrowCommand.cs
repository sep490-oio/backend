using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.Escrows.RefundEscrow;

public sealed record RefundEscrowCommand(Guid EscrowId, string Reason) : ICommand;

internal sealed class RefundEscrowCommandHandler : ICommandHandler<RefundEscrowCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RefundEscrowCommandHandler> _logger;

    public RefundEscrowCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        ILogger<RefundEscrowCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(RefundEscrowCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var escrow = await _dbContext.Set<Escrow>()
            .FirstOrDefaultAsync(e => e.Id.Value == request.EscrowId, cancellationToken);

        if (escrow is null)
            return Error.NotFound("Escrow.NotFound", "Escrow not found.");

        if (escrow.Status != EscrowStatus.Holding)
            return Error.Conflict("Escrow.NotHolding", "Escrow is not in a holding state.");

        var order = await _dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == escrow.OrderId, cancellationToken);

        if (order is null)
            return Error.NotFound("Order.NotFound", "Order associated with escrow not found.");

        var buyerWallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == order.BuyerId, cancellationToken);

        if (buyerWallet is null)
            return Error.NotFound("Wallet.NotFound", "Buyer wallet not found.");

        // 1. Create a Wallet Transaction (Credit) for the Buyer
        var creditResult = buyerWallet.Credit(
            amount: escrow.Amount.Amount,
            transactionId: escrow.HoldTransactionId, // Link back to the original funding transaction
            description: $"Escrow refunded to buyer for Order {order.OrderNumber.Value} - Reason: {request.Reason}",
            nowUtc: now);

        if (creditResult.IsFailure)
            return Error.Conflict("Wallet.CreditFailed", creditResult.Error.Message);

        // 2. Perform Refund on Escrow Domain
        var refundResult = escrow.RefundToBuyer(
            refundTransactionId: escrow.HoldTransactionId.Value,
            createdBy: _currentUser.UserId,
            now: now);

        if (refundResult.IsFailure)
            return Error.Conflict("Escrow.RefundFailed", refundResult.Error.Message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully refunded escrow {EscrowId} to buyer {BuyerId} for order {OrderId}", escrow.Id.Value, order.BuyerId.Value, order.Id.Value);

        return UnitResult.Success<Error>();
    }
}

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

namespace OIO.Application.Context.PaymentContext.Commands.Escrows.ReleaseEscrow;

public sealed record ReleaseEscrowCommand(Guid EscrowId, string Reason) : ICommand;

internal sealed class ReleaseEscrowCommandHandler : ICommandHandler<ReleaseEscrowCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ReleaseEscrowCommandHandler> _logger;

    public ReleaseEscrowCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        ILogger<ReleaseEscrowCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(ReleaseEscrowCommand request, CancellationToken cancellationToken)
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

        var sellerWallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == order.SellerId, cancellationToken);

        if (sellerWallet is null)
            return Error.NotFound("Wallet.NotFound", "Seller wallet not found.");

        // 1. Create a Wallet Transaction (Credit) for the Seller
        // Note: For full tracking we could create an internal Transaction record as well, but Wallet Credit generates a WalletTransaction.
        // We will pass escrow.HoldTransactionId as the reference, or we could generate a new Transaction record for 'EscrowRelease'.
        var creditResult = sellerWallet.Credit(
            amount: escrow.Amount.Amount,
            transactionId: escrow.HoldTransactionId, // Link back to the original funding transaction
            description: $"Escrow released to seller for Order {order.OrderNumber.Value} - Reason: {request.Reason}",
            nowUtc: now);

        if (creditResult.IsFailure)
            return Error.Conflict("Wallet.CreditFailed", creditResult.Error.Message);

        // 2. Perform Release on Escrow Domain
        // For audit purposes, ReleaseTransactionId ideally points to a payout transaction. We reuse HoldTransactionId here for simplicity if we don't have a new transaction record.
        var releaseResult = escrow.ReleaseToSeller(
            releaseTransactionId: escrow.HoldTransactionId.Value,
            createdBy: _currentUser.UserId,
            now: now);

        if (releaseResult.IsFailure)
            return Error.Conflict("Escrow.ReleaseFailed", releaseResult.Error.Message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully released escrow {EscrowId} to seller {SellerId} for order {OrderId}", escrow.Id.Value, order.SellerId.Value, order.Id.Value);

        return UnitResult.Success<Error>();
    }
}

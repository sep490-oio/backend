using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.Escrows;

/// <summary>
/// Release Escrow → Credit tiền vào Wallet của Seller.
/// Được gọi khi buyer confirm nhận hàng hoặc hết grace period.
/// </summary>
public sealed record ReleaseEscrowToSellerCommand(Guid EscrowId) : ICommand;

internal sealed class ReleaseEscrowToSellerCommandHandler
    : ICommandHandler<ReleaseEscrowToSellerCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public ReleaseEscrowToSellerCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<UnitResult<Error>> Handle(
        ReleaseEscrowToSellerCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var actorId = _currentUser.UserId;
        var escrowId = EscrowId.From(request.EscrowId);

        // 1. Tìm Escrow
        var escrow = await _dbContext.Set<Escrow>()
            .Include(e => e.Order)
            .FirstOrDefaultAsync(e => e.Id == escrowId, cancellationToken);

        if (escrow is null)
            return Error.NotFound("Escrow.NotFound", $"Escrow {request.EscrowId} not found.");

        // 2. Tìm Seller Wallet
        var sellerId = escrow.Order.SellerId;
        var sellerWallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == sellerId && w.IsActive, cancellationToken);

        if (sellerWallet is null)
            return Error.NotFound("Wallet.NotFound", "Seller wallet not found.");

        // 3. Tạo Transaction ghi nhận payout
        var txNumberResult = TransactionNumber.Create($"PAYOUT-{Guid.CreateVersion7():N}");
        if (txNumberResult.IsFailure)
            return txNumberResult.Error;

        var txResult = Transaction.Create(
            sellerId,
            txNumberResult.Value,
            TransactionType.Payout,
            escrow.Amount,
            escrow.Currency,
            $"Escrow release payout for Order #{escrow.OrderId.Value}",
            now,
            escrow.OrderId);

        if (txResult.IsFailure)
            return txResult.Error;

        var transaction = txResult.Value;
        transaction.MarkAsCompleted(GatewayInfo.Empty, now);
        _dbContext.Set<Transaction>().Add(transaction);

        // 4. Release Escrow
        var releaseResult = escrow.ReleaseToSeller(transaction.Id, actorId, now);
        if (releaseResult.IsFailure)
            return releaseResult.Error;

        // 5. Credit vào Seller Wallet
        var creditResult = sellerWallet.Credit(
            escrow.Amount.Amount,
            transaction.Id,
            $"Payout from Escrow #{escrow.Id.Value}",
            now);

        if (creditResult.IsFailure)
            return creditResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}

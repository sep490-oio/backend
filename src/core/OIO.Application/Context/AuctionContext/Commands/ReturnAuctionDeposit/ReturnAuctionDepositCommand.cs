using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Descriptions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.ReturnAuctionDeposit;

public sealed record ReturnAuctionDepositCommand(Guid AuctionDepositId, string Reason) : ICommand;

internal sealed class ReturnAuctionDepositCommandHandler : ICommandHandler<ReturnAuctionDepositCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<ReturnAuctionDepositCommandHandler> _logger;

    public ReturnAuctionDepositCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<ReturnAuctionDepositCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(ReturnAuctionDepositCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var auctionDepositId = AuctionDepositId.From(request.AuctionDepositId);

        var deposit = await _dbContext.Set<AuctionDeposit>()
            .FirstOrDefaultAsync(d => d.Id == auctionDepositId, cancellationToken);

        if (deposit is null)
            return Error.NotFound("AuctionDeposit.NotFound", "Deposit not found.");

        if (!deposit.IsHeld)
            return Error.Conflict("AuctionDeposit.NotHeld", "Deposit is not in a holding state.");

        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == deposit.BidderId, cancellationToken);

        if (wallet is null)
            return Error.NotFound("Wallet.NotFound", "Wallet not found for the bidder.");

        // Return logic on the deposit
        var returnResult = deposit.Return(now);
        if (returnResult.IsFailure)
            return Error.Conflict("AuctionDeposit.ReturnFailed", returnResult.Error.Message);

        // Unhold from wallet: moves amount from pending_balance to available balance
        var unholdResult = wallet.Unhold(
            amount: deposit.Amount.Amount,
            transactionId: deposit.TransactionId,
            description: LedgerDescriptions.ReturnedAuctionDeposit(request.Reason, deposit.AuctionId.Value),
            nowUtc: now);

        if (unholdResult.IsFailure)
            return Error.Conflict("Wallet.UnholdFailed", unholdResult.Error.Message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully returned auction deposit {DepositId} to wallet of user {UserId}", deposit.Id.Value, deposit.BidderId.Value);

        return UnitResult.Success<Error>();
    }
}

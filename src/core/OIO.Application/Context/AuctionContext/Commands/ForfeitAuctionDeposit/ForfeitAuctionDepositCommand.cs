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

namespace OIO.Application.Context.AuctionContext.Commands.ForfeitAuctionDeposit;

public sealed record ForfeitAuctionDepositCommand(Guid AuctionDepositId, string Reason) : ICommand;

internal sealed class ForfeitAuctionDepositCommandHandler : ICommandHandler<ForfeitAuctionDepositCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<ForfeitAuctionDepositCommandHandler> _logger;

    public ForfeitAuctionDepositCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<ForfeitAuctionDepositCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(ForfeitAuctionDepositCommand request, CancellationToken cancellationToken)
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

        // Forfeit logic on the deposit
        var forfeitResult = deposit.Forfeit(now);
        if (forfeitResult.IsFailure)
            return Error.Conflict("AuctionDeposit.ForfeitFailed", forfeitResult.Error.Message);

        // DebitPending from wallet: permanently removes amount from pending_balance 
        var debitResult = wallet.DebitPending(
            amount: deposit.Amount.Amount,
            transactionId: deposit.TransactionId,
            description: LedgerDescriptions.ForfeitedAuctionDeposit(request.Reason),
            nowUtc: now);

        if (debitResult.IsFailure)
            return Error.Conflict("Wallet.DebitPendingFailed", debitResult.Error.Message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully forfeited auction deposit {DepositId} from wallet of user {UserId}", deposit.Id.Value, deposit.BidderId.Value);

        return UnitResult.Success<Error>();
    }
}

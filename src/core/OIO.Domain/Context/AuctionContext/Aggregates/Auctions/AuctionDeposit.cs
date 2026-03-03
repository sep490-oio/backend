using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionDeposit : BaseEntity<AuctionDepositId>
{
    public AuctionId AuctionId { get; private set; }
    public UserId UserId { get; private set; }
    public Money Amount { get; private set; }
    public TransactionId? TransactionId { get; private set; }
    public DepositStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    private AuctionDeposit() { }

    private AuctionDeposit(
        AuctionDepositId id,
        AuctionId auctionId,
        UserId userId,
        Money amount,
        TransactionId? transactionId,
        DateTime now)
        : base(id)
    {
        AuctionId = auctionId;
        UserId = userId;
        Amount = amount;
        TransactionId = transactionId;
        Status = DepositStatus.Held;
        CreatedAt = now;
    }

    public static Result<AuctionDeposit, Error> Create(
        AuctionId auctionId,
        UserId userId,
        Money amount,
        TransactionId? transactionId,
        DateTime now)
    {
        if (auctionId.Value == Guid.Empty)
            return AuctionErrors.Deposit.InvalidInput("AuctionId cannot be empty");

        if (userId.Value == Guid.Empty)
            return AuctionErrors.Deposit.InvalidInput("UserId cannot be empty");

        if (amount.Amount <= 0)
            return AuctionErrors.Deposit.InvalidAmount;

        var deposit = new AuctionDeposit(
            AuctionDepositId.From(Guid.CreateVersion7()),
            auctionId,
            userId,
            amount,
            transactionId,
            now);

        return Result.Success<AuctionDeposit, Error>(deposit);
    }

    public UnitResult<Error> Return(DateTime now)
    {
        if (Status != DepositStatus.Held)
            return AuctionErrors.Deposit.CannotReturn;

        Status = DepositStatus.Returned;
        ReleasedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Forfeit(DateTime now)
    {
        if (Status != DepositStatus.Held)
            return AuctionErrors.Deposit.CannotForfeit;

        Status = DepositStatus.Forfeited;
        ReleasedAt = now;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ConvertToPayment(DateTime now)
    {
        if (Status != DepositStatus.Held)
            return AuctionErrors.Deposit.CannotConvert;

        Status = DepositStatus.ConvertedToPayment;
        ReleasedAt = now;

        return UnitResult.Success<Error>();
    }

    public bool IsHeld => Status == DepositStatus.Held;
    public bool IsReleased => ReleasedAt.HasValue;
}
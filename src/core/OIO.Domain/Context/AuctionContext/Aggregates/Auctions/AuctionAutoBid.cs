using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionAutoBid : BaseEntity<AuctionAutoBidId>
{
    public AuctionId AuctionId { get; private set; }
    public UserId BidderId { get; private set; }
    public bool IsEnabled { get; private set; }
    public Money MaxAmount { get; private set; }
    public Money CurrentAmount { get; private set; }
    public Money? IncrementAmount { get; private set; }
    public AutoBidStatus Status { get; private set; }
    public int TotalAutoBids { get; private set; }
    public DateTime? LastAutoBidAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private AuctionAutoBid() { }

    private AuctionAutoBid(
        AuctionAutoBidId id,
        AuctionId auctionId,
        UserId bidderId,
        Money maxAmount,
        Money currentAmount,
        Money? incrementAmount,
        DateTime now)
        : base(id)
    {
        AuctionId = auctionId;
        BidderId = bidderId;
        MaxAmount = maxAmount;
        CurrentAmount = currentAmount;
        IncrementAmount = incrementAmount;
        IsEnabled = true;
        Status = AutoBidStatus.Active;
        TotalAutoBids = 0;
        CreatedAt = now;
    }

    public static Result<AuctionAutoBid, Error> Create(
        AuctionId auctionId,
        UserId bidderId,
        Money maxAmount,
        Money currentAmount,
        Money? incrementAmount,
        DateTime now)
    {
        if (auctionId.Value == Guid.Empty)
            return AuctionErrors.AutoBid.InvalidInput("AuctionId cannot be empty");

        if (bidderId.Value == Guid.Empty)
            return AuctionErrors.AutoBid.InvalidInput("BidderId cannot be empty");

        if (maxAmount.Amount <= 0)
            return AuctionErrors.AutoBid.InvalidMaxAmount;

        if (currentAmount.Amount < 0)
            return AuctionErrors.AutoBid.InvalidInput("CurrentAmount cannot be negative");

        if (currentAmount.Amount > maxAmount.Amount)
            return AuctionErrors.AutoBid.InvalidCurrentAmount;

        if (incrementAmount?.Amount <= 0)
            return AuctionErrors.AutoBid.InvalidInput("IncrementAmount must be greater than 0 or null");

        var autoBid = new AuctionAutoBid(
            AuctionAutoBidId.From(Guid.CreateVersion7()),
            auctionId,
            bidderId,
            maxAmount,
            currentAmount,
            incrementAmount,
            now);

        return Result.Success<AuctionAutoBid, Error>(autoBid);
    }

    public UnitResult<Error> UpdateCurrentAmount(Money newAmount, DateTime now)
    {
        if (!IsEnabled)
            return AuctionErrors.AutoBid.IsDisabled;

        if (Status == AutoBidStatus.Won || Status == AutoBidStatus.Outbid)
            return AuctionErrors.AutoBid.CannotModifyFinalStatus;

        if (newAmount.Amount > MaxAmount.Amount)
            return AuctionErrors.AutoBid.ExceedsMaxAmount(MaxAmount.Amount);

        CurrentAmount = newAmount;
        TotalAutoBids++;
        LastAutoBidAt = now;
        ModifiedAt = now;

        if (CurrentAmount.Amount >= MaxAmount.Amount)
        {
            Status = AutoBidStatus.Exhausted;
            IsEnabled = false;
        }

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Pause()
    {
        if (Status != AutoBidStatus.Active)
            return AuctionErrors.AutoBid.CannotPause;

        Status = AutoBidStatus.Paused;
        IsEnabled = false;
        ModifiedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Resume()
    {
        if (Status != AutoBidStatus.Paused)
            return AuctionErrors.AutoBid.CannotResume;

        Status = AutoBidStatus.Active;
        IsEnabled = true;
        ModifiedAt = DateTime.UtcNow;

        return UnitResult.Success<Error>();
    }

    public void MarkAsWon(DateTime now)
    {
        Status = AutoBidStatus.Won;
        IsEnabled = false;
        ModifiedAt = now;
    }

    public void MarkAsOutbid(DateTime now)
    {
        Status = AutoBidStatus.Outbid;
        IsEnabled = false;
        ModifiedAt = now;
    }
}
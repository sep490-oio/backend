using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AutoBid : BaseEntity<AutoBidId>, IAuditableEntity
{
    public AuctionId AuctionId { get; private set; }
    public UserId BidderId { get; private set; }
    public Money MaxAmount { get; private set; }
    public Money CurrentAmount { get; private set; }
    public Money? IncrementAmount { get; private set; }
    public bool IsEnabled { get; private set; }
    public AutoBidStatus Status { get; private set; }
    public int TotalAutoBids { get; private set; }
    public DateTime? LastAutoBidAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private AutoBid() { }

    private AutoBid(
        AutoBidId id,
        AuctionId auctionId,
        UserId bidderId,
        Money maxAmount,
        Money currentAmount,
        Money? incrementAmount,
        DateTime nowUtc)
        : base(id)
    {
        AuctionId = auctionId;
        BidderId = bidderId;
        MaxAmount = maxAmount;
        CurrentAmount = currentAmount;
        IncrementAmount = incrementAmount;
        Status = AutoBidStatus.Active;
        TotalAutoBids = 0;
        CreatedAt = nowUtc;
    }

    public static AutoBid Create(
        AuctionId auctionId,
        UserId bidderId,
        Money maxAmount,
        Money currentAmount,
        Money? incrementAmount,
        DateTime nowUtc)
    {
        var autoBid = new AutoBid(
            AutoBidId.From(Guid.CreateVersion7()),
            auctionId,
            bidderId,
            maxAmount,
            currentAmount,
            incrementAmount,
            nowUtc);

        return autoBid;
    }
    
    public bool CanBid(Money requiredAmount) =>
        IsEnabled &&
        Status == AutoBidStatus.Active &&
        MaxAmount.IsGreaterThanOrEqual(requiredAmount);

    public Result<Money, Error> CalculateNextBidAmount(Money minimumRequired)
    {
        if (!CanBid(minimumRequired))
            return AuctionErrors.AutoBid.CannotBid;

        var desiredAmount = IncrementAmount is not null
            ? CurrentAmount + IncrementAmount
            : minimumRequired;

        if (desiredAmount < minimumRequired)
            desiredAmount = minimumRequired;

        // Cap at max amount
        if (desiredAmount > MaxAmount)
            desiredAmount = MaxAmount;

        return desiredAmount;
    }
    
    public UnitResult<Error> UpdateCurrentAmount(Money newAmount, DateTime nowUtc)
    {
        if (!IsEnabled)
            return AuctionErrors.AutoBid.IsDisabled;

        if (Status == AutoBidStatus.Won || Status == AutoBidStatus.Outbid)
            return AuctionErrors.AutoBid.CannotModifyFinalStatus;

        if (newAmount.Amount > MaxAmount.Amount)
            return AuctionErrors.AutoBid.ExceedsMaxAmount(MaxAmount.Amount);

        CurrentAmount = newAmount;
        TotalAutoBids++;
        LastAutoBidAt = nowUtc;
        ModifiedAt = nowUtc;

        if (CurrentAmount.Amount < MaxAmount.Amount) 
            return UnitResult.Success<Error>();
        
        MarkAsExhausted(nowUtc);

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> UpdateConfig(
        Money newMaxAmount, 
        Money? newIncrementAmount, 
        DateTime nowUtc)
    {
        if (!IsEnabled && Status != AutoBidStatus.Exhausted)
            return AuctionErrors.AutoBid.IsDisabled;

        if (Status == AutoBidStatus.Won)
            return AuctionErrors.AutoBid.CannotModifyFinalStatus;

        if (newMaxAmount.Amount < CurrentAmount.Amount)
            return AuctionErrors.AutoBid.NewMaxLessThanCurrent(CurrentAmount.Amount);
        
        if (Status == AutoBidStatus.Exhausted || Status == AutoBidStatus.Outbid)
            Status = AutoBidStatus.Active;
        
        MaxAmount = newMaxAmount;
        ModifiedAt = nowUtc;
        IncrementAmount = newIncrementAmount;
        IsEnabled = true;
        
        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> Pause(DateTime nowUtc)
    {
        if (Status != AutoBidStatus.Active)
            return AuctionErrors.AutoBid.CannotPause;

        Status = AutoBidStatus.Paused;
        ModifiedAt = nowUtc;
        IsEnabled = false;
        
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Resume(DateTime nowUtc)
    {
        if (Status != AutoBidStatus.Paused)
            return AuctionErrors.AutoBid.CannotResume;

        Status = AutoBidStatus.Active;
        ModifiedAt = nowUtc;
        IsEnabled = true;
        
        return UnitResult.Success<Error>();
    }

    public void MarkAsWon(DateTime nowUtc)
    {
        Status = AutoBidStatus.Won;
        IsEnabled = false;
        ModifiedAt = nowUtc;
    }

    public void MarkAsOutbid(DateTime nowUtc)
    {
        Status = AutoBidStatus.Outbid;
        ModifiedAt = nowUtc;
    }
    
    public void MarkAsExhausted(DateTime nowUtc)
    {
        Status = AutoBidStatus.Exhausted;
        ModifiedAt = nowUtc;
    }
    
}
using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AutoBid : BaseEntity<AutoBidId>, IAuditableEntity
{
    private readonly List<Bid> _bids = [];
    public AuctionId AuctionId { get; private set; }
    public UserId BidderId { get; private set; }
    public bool IsEnabled { get; private set; }
    
    public AutoBidBudget Budget { get; private set; }
    public AutoBidStatus Status { get; private set; }
    public int TotalAutoBids { get; private set; }
    public DateTime? LastAutoBidAt { get; private set; }
    public string? StopReason { get; private set; }
    public DateTime? StoppedAt { get; private set; }
    public DateTime? LastValidationAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public Auction Auction { get; private set; } = null!;
    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();

    private AutoBid() { }

    private AutoBid(
        AutoBidId id,
        AuctionId auctionId,
        UserId bidderId,
        AutoBidBudget budget,
        DateTime nowUtc)
        : base(id)
    {
        AuctionId = auctionId;
        BidderId = bidderId;
        IsEnabled = true;
        Budget = budget;
        Status = AutoBidStatus.Active;
        TotalAutoBids = 0;
        CreatedAt = nowUtc;
    }

    public static AutoBid Create(
        AuctionId auctionId,
        UserId bidderId,
        AutoBidBudget budget,
        DateTime nowUtc)
    {
        var autoBid = new AutoBid(
            AutoBidId.From(Guid.CreateVersion7()),
            auctionId,
            bidderId,
            budget,
            nowUtc);

        return autoBid;
    }
    
    public bool CanBid(Money requiredAmount) =>
        IsEnabled &&
        Status == AutoBidStatus.Active &&
        Budget.MaxPrice.IsGreaterThanOrEqual(requiredAmount);

    public decimal HeldAmount { get; private set; }

    public void SetHeldAmount(decimal amount)
    {
        HeldAmount = amount;
    }

    public Result<Money, Error> CalculateNextBidAmount(Money minimumRequired)
    {
        if (!CanBid(minimumRequired))
            return AuctionErrors.AutoBid.CannotBid;

        var desiredAmount = Budget.Increment is not null
            ? Budget.CurrentPrice + Budget.Increment
            : minimumRequired;

        if (desiredAmount < minimumRequired)
            desiredAmount = minimumRequired;

        // Cap at max amount
        if (desiredAmount > Budget.MaxPrice)
            desiredAmount = Budget.MaxPrice;

        return desiredAmount;
    }
    
    public UnitResult<Error> UpdateCurrentAmount(Money newAmount, DateTime nowUtc)
    {
        if (!IsEnabled)
            return AuctionErrors.AutoBid.IsDisabled;

        if (Status == AutoBidStatus.Won || Status == AutoBidStatus.Outbid)
            return AuctionErrors.AutoBid.CannotModifyFinalStatus;

        if (newAmount.Amount > Budget.MaxAmount)
            return AuctionErrors.AutoBid.ExceedsMaxAmount(Budget.MaxAmount);

        var result = Budget.WithBidPlaced(newAmount);
        
        if(result.IsFailure)
            return result.Error;

        Budget = result.Value;
        TotalAutoBids++;
        LastAutoBidAt = nowUtc;
        LastValidationAt = nowUtc;
        ModifiedAt = nowUtc;
        StopReason = null;
        StoppedAt = null;

        if (Budget.IsExhausted)
            MarkAsExhausted(nowUtc);

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> UpdateConfig(
        Money newMaxAmount, 
        Money? newIncrementAmount, 
        DateTime nowUtc)
    {
        if (!IsEnabled &&
            Status != AutoBidStatus.Exhausted &&
            Status != AutoBidStatus.Outbid)
            return AuctionErrors.AutoBid.IsDisabled;

        if (Status == AutoBidStatus.Won)
            return AuctionErrors.AutoBid.CannotModifyFinalStatus;

        if (newMaxAmount.Amount < Budget.CurrentAmount)
            return AuctionErrors.AutoBid.NewMaxLessThanCurrent(Budget.CurrentAmount);
        
        var result = Budget.WithConfiguration(newMaxAmount, newIncrementAmount);
        
        if (result.IsFailure)
        {
            return result.Error;
        }
        
        Budget = result.Value;
        if (Status == AutoBidStatus.Exhausted || Status == AutoBidStatus.Outbid)
            Status = AutoBidStatus.Active;
        ModifiedAt = nowUtc;
        IsEnabled = true;
        StopReason = null;
        StoppedAt = null;
        LastValidationAt = nowUtc;
        
        return UnitResult.Success<Error>();
    }
    
    /// <summary>
    /// Cancel auto-bid — disables bidding and releases wallet hold immediately.
    /// This is a terminal state; the auto-bid cannot be reactivated after cancellation.
    /// </summary>
    public UnitResult<Error> Cancel(DateTime nowUtc)
    {
        if (Status == AutoBidStatus.Cancelled)
            return UnitResult.Failure(Error.Conflict("AutoBid.AlreadyCancelled", "Auto-bid is already cancelled."));

        Status = AutoBidStatus.Cancelled;
        IsEnabled = false;
        StopReason = "cancelled_by_user";
        StoppedAt = nowUtc;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Pause auto-bid — disables bidding but keeps wallet hold.
    /// Wallet reservation is retained so Resume can reactivate instantly without re-hold.
    /// Funds are only released when auction ends (Sold/Failed/Cancelled/Terminated).
    /// </summary>
    public UnitResult<Error> Pause(DateTime nowUtc)
    {
        if (Status != AutoBidStatus.Active)
            return AuctionErrors.AutoBid.CannotPause;

        Status = AutoBidStatus.Paused;
        ModifiedAt = nowUtc;
        IsEnabled = false;
        StopReason = "paused_by_user";
        StoppedAt = nowUtc;
        
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Resume(DateTime nowUtc)
    {
        if (Status != AutoBidStatus.Paused)
            return AuctionErrors.AutoBid.CannotResume;

        Status = AutoBidStatus.Active;
        ModifiedAt = nowUtc;
        IsEnabled = true;
        StopReason = null;
        StoppedAt = null;
        LastValidationAt = nowUtc;
        
        return UnitResult.Success<Error>();
    }

    public void MarkAsWon(DateTime nowUtc)
    {
        Status = AutoBidStatus.Won;
        IsEnabled = false;
        ModifiedAt = nowUtc;
        StopReason = "won";
        StoppedAt = nowUtc;
    }

    public void MarkAsOutbid(DateTime nowUtc)
    {
        Status = AutoBidStatus.Outbid;
        IsEnabled = false;
        ModifiedAt = nowUtc;
        StopReason = "outbid";
        StoppedAt = nowUtc;
    }
    
    public void MarkAsExhausted(DateTime nowUtc)
    {
        Status = AutoBidStatus.Exhausted;
        IsEnabled = false;
        ModifiedAt = nowUtc;
        StopReason = "budget_exhausted";
        StoppedAt = nowUtc;
    }
    
}

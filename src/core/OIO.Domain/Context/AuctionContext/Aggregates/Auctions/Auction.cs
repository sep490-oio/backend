using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class Auction : AggregateRoot<AuctionId>, IAuditableEntity
{
    private readonly List<AuctionPriceHistory> _priceHistories = [];

    // --- Properties khớp 100% với Script DB ---
    public ItemId ItemId { get; private set; }
    public WinningConditions Conditions { get; private set; } // Gồm: starting_price, reserve_price, buy_now_price
    public Money CurrentPrice { get; private set; }           // current_price
    public BidIncrement Increment { get; private set; }      // bid_increment
    public AuctionPeriod Period { get; private set; }        // Gồm: start_time, end_time
    public DateTime? ActualEndTime { get; private set; }     // actual_end_time
    
    public AuctionStatus Status { get; private set; }        // status
    public Guid? CurrentWinnerId { get; private set; }       // winner_id
    
    public bool AutoExtend { get; private set; }             // auto_extend
    public int ExtensionMinutes { get; private set; }        // extension_minutes
    public bool IsFeatured { get; private set; }             // is_featured
    
    public int ViewCount { get; private set; }               // view_count
    public int BidCount { get; private set; }                // bid_count
    public int WatchCount { get; private set; }              // watch_count

    public DateTime CreatedAt { get; private set; }          // created_at
    public DateTime? ModifiedAt { get; private set; }        // modified_at

    // Navigation cho EF Core
    public IReadOnlyCollection<AuctionPriceHistory> PriceHistories => _priceHistories.AsReadOnly();

    private Auction() { }

    private Auction(
        AuctionId id,
        ItemId itemId,
        AuctionPeriod period,
        WinningConditions conditions,
        BidIncrement increment,
        bool autoExtend,
        int extensionMinutes,
        DateTime now)
    {
        Id = id;
        ItemId = itemId;
        Period = period;
        Conditions = conditions;
        Increment = increment;
        AutoExtend = autoExtend;
        ExtensionMinutes = extensionMinutes;
        
        Status = AuctionStatus.Draft;
        CurrentPrice = conditions.StartingPrice;
        
        BidCount = 0;
        ViewCount = 0;
        WatchCount = 0;
        IsFeatured = false;
        CreatedAt = now;
    }

    /// <summary>
    /// Khởi tạo phiên đấu giá (Tích hợp các Checks từ DB)
    /// </summary>
    public static Result<Auction, Error> Create(
        ItemId itemId,
        AuctionPeriod period,
        WinningConditions conditions,
        BidIncrement increment,
        bool autoExtend,
        int extensionMinutes,
        DateTime now)
    {
        // Check: (buy_now_price > starting_price)
        if (conditions.BuyNowPrice != null && conditions.BuyNowPrice.Amount <= conditions.StartingPrice.Amount)
            return Error.Validation("BuyNowPrice", "Auction.InvalidBuyNow", "Giá mua ngay phải lớn hơn giá khởi điểm.");

        // Check: (bid_increment > 0)
        if (increment.Value.Amount <= 0)
            return Error.Validation("Increment", "Auction.InvalidIncrement", "Bước giá phải lớn hơn 0.");

        // Check: (end_time > start_time)
        if (period.EndTime <= period.StartTime)
            return Error.Validation("EndTime", "Auction.InvalidPeriod", "Thời gian kết thúc phải sau thời gian bắt đầu.");

        // Check: (reserve_price >= starting_price)
        if (conditions.ReservePrice != null && conditions.ReservePrice.Amount < conditions.StartingPrice.Amount)
            return Error.Validation("ReservePrice", "Auction.InvalidReserve", "Giá sàn phải lớn hơn hoặc bằng giá khởi điểm.");

        var auction = new Auction(
            AuctionId.From(Guid.CreateVersion7()), 
            itemId, period, conditions, increment, autoExtend, extensionMinutes, now);

        auction.RaiseDomainEvent(new AuctionCreatedEvent(auction.Id.ToString(), itemId.ToString(), now));

        return auction;
    }

    /// <summary>
    /// Xử lý đặt giá và Tự động gia hạn (Auto Extend)
    /// </summary>
    public UnitResult<Error> PlaceBid(Guid bidderId, Money amount, BidId bidId, DateTime nowUtc)
    {
        // Validation cơ bản
        if (Status != AuctionStatus.Active)
            return Error.Conflict("Auction.NotActive", "Phiên đấu giá không trong trạng thái hoạt động.");

        if (!Period.IsActive(nowUtc))
            return Error.Validation("Time", "Auction.Expired", "Thời gian đấu giá đã kết thúc.");

        // Check: (current_price >= starting_price) được đảm bảo qua bước giá
        var minRequired = CurrentPrice.Amount + Increment.Value.Amount;
        if (amount.Amount < minRequired)
            return Error.Validation("Amount", "Auction.BidTooLow", $"Giá đặt tối thiểu là {minRequired}");

        // Cập nhật trạng thái
        CurrentPrice = amount;
        CurrentWinnerId = bidderId;
        BidCount++;
        ModifiedAt = nowUtc;

        // Logic Auto Extend: Nếu bid trong khoảng thời gian extension_minutes cuối cùng
        if (AutoExtend && Period.EndTime.Subtract(nowUtc).TotalMinutes < ExtensionMinutes)
        {
            var extendedEndTime = nowUtc.AddMinutes(ExtensionMinutes);
            Period = AuctionPeriod.Create(Period.StartTime, extendedEndTime).Value;
            // Lưu ý: ActualEndTime thường dùng khi kết thúc thực tế, EndTime dùng để hiển thị/gia hạn
        }

        // Lưu lịch sử (auction_price_history)
        _priceHistories.Add(new AuctionPriceHistory(
            AuctionPriceHistoryId.From(Guid.CreateVersion7()),
            Id, bidderId, amount, nowUtc, bidId));

        // Nếu chạm giá Buy Now thì kết thúc sớm
        if (Conditions.BuyNowPrice != null && amount.Amount >= Conditions.BuyNowPrice.Amount)
        {
            EndAuction(AuctionStatus.Ended, nowUtc, bidderId);
        }

        RaiseDomainEvent(new BidPlacedEvent(Id.ToString(), bidderId, amount, nowUtc));

        return UnitResult.Success<Error>();
    }

    public void EndAuction(AuctionStatus finalStatus, DateTime now, Guid? winnerId = null)
    {
        Status = finalStatus;
        ActualEndTime = now;
        if (winnerId.HasValue) CurrentWinnerId = winnerId;
        ModifiedAt = now;
    }

    public void IncrementView() => ViewCount++;
}
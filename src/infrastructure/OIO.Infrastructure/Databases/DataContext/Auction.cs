using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Auction
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public decimal StartingPrice { get; set; }

    public decimal? ReservePrice { get; set; }

    public decimal? BuyNowPrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal BidIncrement { get; set; }

    public string? Currency { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public DateTime? ActualEndTime { get; set; }

    public string Status { get; set; } = null!;

    public Guid? WinnerId { get; set; }

    public bool? AutoExtend { get; set; }

    public int? ExtensionMinutes { get; set; }

    public bool? IsFeatured { get; set; }

    public int? ViewCount { get; set; }

    public int? BidCount { get; set; }

    public int? WatchCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<AuctionAutoBid> AuctionAutoBids { get; set; } = new List<AuctionAutoBid>();

    public virtual ICollection<AuctionDeposit> AuctionDeposits { get; set; } = new List<AuctionDeposit>();

    public virtual ICollection<AuctionPriceHistory> AuctionPriceHistories { get; set; } = new List<AuctionPriceHistory>();

    public virtual ICollection<AuctionWatcher> AuctionWatchers { get; set; } = new List<AuctionWatcher>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual Item Item { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<SellerReview> SellerReviews { get; set; } = new List<SellerReview>();

    public virtual User? Winner { get; set; }
}

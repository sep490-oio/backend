using System;
using System.Collections.Generic;
using System.Net;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Bid
{
    public Guid Id { get; set; }

    public Guid AuctionId { get; set; }

    public Guid BidderId { get; set; }

    public decimal Amount { get; set; }

    public bool? IsAutoBid { get; set; }

    public Guid? AutoBidId { get; set; }

    public string Status { get; set; } = null!;

    public IPAddress? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual ICollection<AuctionPriceHistory> AuctionPriceHistories { get; set; } = new List<AuctionPriceHistory>();

    public virtual AuctionAutoBid? AutoBid { get; set; }

    public virtual User Bidder { get; set; } = null!;
}

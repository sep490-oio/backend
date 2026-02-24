using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class AuctionPriceHistory
{
    public Guid Id { get; set; }

    public Guid AuctionId { get; set; }

    public decimal Price { get; set; }

    public Guid? BidId { get; set; }

    public DateTime RecordedAt { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual Bid? Bid { get; set; }
}

using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class AuctionAutoBid
{
    public Guid Id { get; set; }

    public Guid AuctionId { get; set; }

    public Guid BidderId { get; set; }

    public bool IsEnabled { get; set; }

    public decimal MaxAmount { get; set; }

    public decimal CurrentAmount { get; set; }

    public decimal? IncrementAmount { get; set; }

    public string Status { get; set; } = null!;

    public int TotalAutoBids { get; set; }

    public DateTime? LastAutoBidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual User Bidder { get; set; } = null!;

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();
}

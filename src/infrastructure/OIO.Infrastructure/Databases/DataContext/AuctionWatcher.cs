using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class AuctionWatcher
{
    public Guid Id { get; set; }

    public Guid AuctionId { get; set; }

    public Guid UserId { get; set; }

    public bool? NotifyOnBid { get; set; }

    public bool? NotifyOnEnd { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

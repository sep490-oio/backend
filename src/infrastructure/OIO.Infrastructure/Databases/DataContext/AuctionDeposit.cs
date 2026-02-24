using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class AuctionDeposit
{
    public Guid Id { get; set; }

    public Guid AuctionId { get; set; }

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public Guid? TransactionId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual Transaction? Transaction { get; set; }

    public virtual User User { get; set; } = null!;
}

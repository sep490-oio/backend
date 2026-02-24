using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class SellerReview
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid AuctionId { get; set; }

    public Guid ReviewerId { get; set; }

    public Guid SellerId { get; set; }

    public short OverallRating { get; set; }

    public short? CommunicationRating { get; set; }

    public short? ShippingSpeedRating { get; set; }

    public short? ItemAccuracyRating { get; set; }

    public string? Title { get; set; }

    public string? Comment { get; set; }

    public bool? IsVerifiedPurchase { get; set; }

    public string? Status { get; set; }

    public string? ModerationReason { get; set; }

    public string? SellerResponse { get; set; }

    public DateTime? SellerRespondedAt { get; set; }

    public int? HelpfulCount { get; set; }

    public int? NotHelpfulCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ReviewImage> ReviewImages { get; set; } = new List<ReviewImage>();

    public virtual ICollection<ReviewReport> ReviewReports { get; set; } = new List<ReviewReport>();

    public virtual ICollection<ReviewVote> ReviewVotes { get; set; } = new List<ReviewVote>();

    public virtual User Reviewer { get; set; } = null!;

    public virtual User Seller { get; set; } = null!;
}

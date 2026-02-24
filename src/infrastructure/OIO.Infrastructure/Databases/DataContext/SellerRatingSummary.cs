using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class SellerRatingSummary
{
    public Guid Id { get; set; }

    public Guid SellerId { get; set; }

    public int? TotalReviews { get; set; }

    public decimal? AverageRating { get; set; }

    public int? Rating5Count { get; set; }

    public int? Rating4Count { get; set; }

    public int? Rating3Count { get; set; }

    public int? Rating2Count { get; set; }

    public int? Rating1Count { get; set; }

    public decimal? AvgCommunication { get; set; }

    public decimal? AvgShippingSpeed { get; set; }

    public decimal? AvgItemAccuracy { get; set; }

    public int? ResponseCount { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public virtual User Seller { get; set; } = null!;
}

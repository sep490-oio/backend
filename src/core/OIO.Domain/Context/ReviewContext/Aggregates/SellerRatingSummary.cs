using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ReviewContext.Aggregates;

public sealed class SellerRatingSummary : BaseEntity<SellerRatingSummaryId>
{
    public UserId SellerId { get; private set; }
    public int TotalReviews { get; private set; }
    public decimal AverageRating { get; private set; }
    public int Rating5Count { get; private set; }
    public int Rating4Count { get; private set; }
    public int Rating3Count { get; private set; }
    public int Rating2Count { get; private set; }
    public int Rating1Count { get; private set; }
    public decimal AvgCommunication { get; private set; }
    public decimal AvgShippingSpeed { get; private set; }
    public decimal AvgItemAccuracy { get; private set; }
    public int ResponseCount { get; private set; }
    public DateTime? LastUpdatedAt { get; private set; }

    private SellerRatingSummary() { }
}
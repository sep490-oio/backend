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

    public SellerRatingSummary(UserId sellerId)
    {
        Id = SellerRatingSummaryId.From(Guid.NewGuid());
        SellerId = sellerId;
    }

    public void ApplyReview(SellerReview review)
    {
        TotalReviews++;
        
        // Recompute averages: newAvg = ((oldAvg * (TotalReviews - 1)) + newValue) / TotalReviews
        AverageRating = ((AverageRating * (TotalReviews - 1)) + review.OverallRating.Value) / TotalReviews;

        if (review.CommunicationRating != null)
        {
            AvgCommunication = ((AvgCommunication * (TotalReviews - 1)) + review.CommunicationRating.Value) / TotalReviews;
        }

        if (review.ShippingSpeedRating != null)
        {
            AvgShippingSpeed = ((AvgShippingSpeed * (TotalReviews - 1)) + review.ShippingSpeedRating.Value) / TotalReviews;
        }

        if (review.ItemAccuracyRating != null)
        {
            AvgItemAccuracy = ((AvgItemAccuracy * (TotalReviews - 1)) + review.ItemAccuracyRating.Value) / TotalReviews;
        }

        switch (review.OverallRating.Value)
        {
            case 5: Rating5Count++; break;
            case 4: Rating4Count++; break;
            case 3: Rating3Count++; break;
            case 2: Rating2Count++; break;
            case 1: Rating1Count++; break;
        }

        LastUpdatedAt = DateTime.UtcNow;
    }
}
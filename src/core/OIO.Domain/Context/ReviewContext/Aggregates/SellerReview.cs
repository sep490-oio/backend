using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.ReviewContext.Enums;
using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ReviewContext.Aggregates;

public sealed class SellerReview : AggregateRoot<SellerReviewId>, IAuditableEntity
{
    private readonly List<ReviewMedia> _media = [];
    private readonly List<ReviewVote> _votes = [];
    private readonly List<ReviewReport> _reports = [];

    public OrderId OrderId { get; private set; }
    public AuctionId AuctionId { get; private set; }
    public UserId ReviewerId { get; private set; }
    public UserId SellerId { get; private set; }
    public Rating OverallRating { get; private set; }         // 1–5
    public Rating? CommunicationRating { get; private set; }  // 1–5
    public Rating? ShippingSpeedRating { get; private set; }  // 1–5
    public Rating? ItemAccuracyRating { get; private set; }   // 1–5
    public string? Title { get; private set; }
    public string? Comment { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public ReviewStatus Status { get; private set; }
    public string? ModerationReason { get; private set; }
    public string? SellerResponse { get; private set; }
    public DateTime? SellerRespondedAt { get; private set; }
    public int HelpfulCount { get; private set; }
    public int NotHelpfulCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public IReadOnlyCollection<ReviewMedia> Media => _media.AsReadOnly();
    public IReadOnlyCollection<ReviewVote> Votes => _votes.AsReadOnly();
    public IReadOnlyCollection<ReviewReport> Reports => _reports.AsReadOnly();

    private SellerReview() { }

    public static SellerReview Create(
        OrderId orderId,
        AuctionId auctionId,
        UserId reviewerId,
        UserId sellerId,
        Rating overallRating,
        Rating? communicationRating,
        Rating? shippingSpeedRating,
        Rating? itemAccuracyRating,
        string? title,
        string? comment,
        bool isVerifiedPurchase,
        DateTime createdAt)
    {
        var review = new SellerReview
        {
            Id = SellerReviewId.From(Guid.NewGuid()),
            OrderId = orderId,
            AuctionId = auctionId,
            ReviewerId = reviewerId,
            SellerId = sellerId,
            OverallRating = overallRating,
            CommunicationRating = communicationRating,
            ShippingSpeedRating = shippingSpeedRating,
            ItemAccuracyRating = itemAccuracyRating,
            Title = title,
            Comment = comment,
            IsVerifiedPurchase = isVerifiedPurchase,
            Status = ReviewStatus.Published,
            HelpfulCount = 0,
            NotHelpfulCount = 0,
            CreatedAt = createdAt
        };

        return review;
    }
}
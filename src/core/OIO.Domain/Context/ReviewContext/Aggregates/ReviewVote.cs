using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ReviewContext.Aggregates;

public sealed class ReviewVote : BaseEntity<ReviewVoteId>, ICreatedAtEntity
{
    public SellerReviewId ReviewId { get; private set; }
    public UserId UserId { get; private set; }
    public bool IsHelpful { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public SellerReview Review { get; private set; } = null!;
    private ReviewVote() { }
}
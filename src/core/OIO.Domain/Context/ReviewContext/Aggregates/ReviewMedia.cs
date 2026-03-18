using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ReviewContext.Aggregates;

public sealed class ReviewMedia : BaseEntity<ReviewImageId>, ICreatedAtEntity
{
    public SellerReviewId ReviewId { get; private set; }
    public string Url { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public SellerReview Review { get; private set; } = null!;
    private ReviewMedia() { }
}
using OIO.Domain.Context.ReviewContext.Enums;
using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ReviewContext.Aggregates;

public sealed class ReviewReport : BaseEntity<ReviewReportId>, ICreatedAtEntity
{
    public SellerReviewId ReviewId { get; private set; }
    public UserId ReporterId { get; private set; }
    public ReviewReportReason Reason { get; private set; }
    public string? Description { get; private set; }
    public ReviewReportStatus Status { get; private set; }
    public UserId? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public SellerReview Review { get; private set; } = null!;
    private ReviewReport() { }
}
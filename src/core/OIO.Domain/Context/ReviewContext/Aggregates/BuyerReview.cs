using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.ReviewContext.Enums;
using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ReviewContext.Aggregates;

public sealed class BuyerReview : AggregateRoot<BuyerReviewId>, ICreatedAtEntity
{
    public OrderId OrderId { get; private set; }
    public UserId ReviewerId { get; private set; }
    public UserId BuyerId { get; private set; }
    public Rating OverallRating { get; private set; }           // 1–5
    public Rating? PaymentSpeedRating { get; private set; }     // 1–5
    public Rating? CommunicationRating { get; private set; }    // 1–5
    public string? Comment { get; private set; }
    public ReviewStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BuyerReview() { }
}
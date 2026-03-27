namespace OIO.Application.Context.ReviewContext.DTOs;

public sealed record SellerReviewDto(
    Guid Id,
    Guid OrderId,
    Guid ReviewerId,
    string ReviewerName,
    Guid SellerId,
    int OverallRating,
    int? CommunicationRating,
    int? ShippingSpeedRating,
    int? ItemAccuracyRating,
    string? Title,
    string? Comment,
    DateTime CreatedAt);

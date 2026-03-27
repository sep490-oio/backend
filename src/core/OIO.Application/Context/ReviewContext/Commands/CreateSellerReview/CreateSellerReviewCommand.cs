using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ReviewContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ReviewContext.Commands.CreateSellerReview;

public sealed record CreateSellerReviewCommand(
    Guid OrderId,
    int OverallRating,
    int? CommunicationRating,
    int? ShippingSpeedRating,
    int? ItemAccuracyRating,
    string? Title,
    string? Comment) : ICommand<SellerReviewDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CreateSellerReviewCommand.Check()
            .WithOwnerName("CreateSellerReview")
            .Field(OrderId).NotEmptyGuid()
            .Field(OverallRating).BetweenInclusive(1, 5);
}

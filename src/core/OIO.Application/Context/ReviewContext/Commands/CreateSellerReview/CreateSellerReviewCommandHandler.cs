using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ReviewContext.DTOs;
using OIO.Application.Context.ReviewContext.Errors;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.ReviewContext.Aggregates;
using OIO.Domain.Context.ReviewContext.Enums;
using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ReviewContext.Commands.CreateSellerReview;

internal sealed class CreateSellerReviewCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<CreateSellerReviewCommand, SellerReviewDto>
{
    public async Task<Result<SellerReviewDto, Error>> Handle(
        CreateSellerReviewCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(request.OrderId);

        // 1. Load the order
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(orderId);

        // 2. Verify the current user is the buyer
        if (order.BuyerId != currentUser.UserId)
            return ReviewErrors.SellerReview.NotBuyer(orderId);

        // 3. Verify order is completed
        if (order.Status != OrderStatus.Completed)
            return ReviewErrors.SellerReview.OrderNotCompleted(orderId);

        // 4. Verify delivery was confirmed
        if (order.DeliveredAt is null)
            return ReviewErrors.SellerReview.DeliveryNotConfirmed(orderId);

        // 5. Check no existing review from this user for this order
        var existingReview = await dbContext.Set<SellerReview>()
            .AsNoTracking()
            .AnyAsync(r => r.OrderId == orderId && r.ReviewerId == currentUser.UserId, cancellationToken);

        if (existingReview)
            return ReviewErrors.SellerReview.AlreadyReviewed(orderId, currentUser.UserId);

        // 6. Create Rating value objects
        var overallRatingResult = Rating.Create((short)request.OverallRating);
        if (overallRatingResult.IsFailure)
            return overallRatingResult.Error;

        Rating? communicationRating = null;
        if (request.CommunicationRating.HasValue)
        {
            var result = Rating.Create((short)request.CommunicationRating.Value);
            if (result.IsFailure) return result.Error;
            communicationRating = result.Value;
        }

        Rating? shippingSpeedRating = null;
        if (request.ShippingSpeedRating.HasValue)
        {
            var result = Rating.Create((short)request.ShippingSpeedRating.Value);
            if (result.IsFailure) return result.Error;
            shippingSpeedRating = result.Value;
        }

        Rating? itemAccuracyRating = null;
        if (request.ItemAccuracyRating.HasValue)
        {
            var result = Rating.Create((short)request.ItemAccuracyRating.Value);
            if (result.IsFailure) return result.Error;
            itemAccuracyRating = result.Value;
        }

        // 7. Create the review entity
        var review = SellerReview.Create(
            orderId: orderId,
            auctionId: order.AuctionId,
            reviewerId: currentUser.UserId,
            sellerId: order.SellerId,
            overallRating: overallRatingResult.Value,
            communicationRating: communicationRating,
            shippingSpeedRating: shippingSpeedRating,
            itemAccuracyRating: itemAccuracyRating,
            title: request.Title,
            comment: request.Comment,
            isVerifiedPurchase: true,
            createdAt: clock.UtcNow);

        // 8. Persist
        dbContext.Insert(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 9. Get reviewer name for DTO
        var reviewer = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.UserName!.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return new SellerReviewDto(
            Id: review.Id.Value,
            OrderId: review.OrderId.Value,
            ReviewerId: review.ReviewerId.Value,
            ReviewerName: reviewer ?? string.Empty,
            SellerId: review.SellerId.Value,
            OverallRating: review.OverallRating.Value,
            CommunicationRating: review.CommunicationRating?.Value,
            ShippingSpeedRating: review.ShippingSpeedRating?.Value,
            ItemAccuracyRating: review.ItemAccuracyRating?.Value,
            Title: review.Title,
            Comment: review.Comment,
            CreatedAt: review.CreatedAt);
    }
}

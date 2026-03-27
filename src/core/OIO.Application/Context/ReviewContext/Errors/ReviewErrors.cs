using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ReviewContext.Errors;

public static class ReviewErrors
{
    public static class SellerReview
    {
        public static Error OrderNotCompleted(OrderId orderId)
            => Error.Validation(string.Empty, "SellerReview.OrderNotCompleted",
                $"Cannot review because order {orderId} is not in completed state.");

        public static Error DeliveryNotConfirmed(OrderId orderId)
            => Error.Validation(string.Empty, "SellerReview.DeliveryNotConfirmed",
                $"Cannot review because order {orderId} delivery has not been confirmed.");

        public static Error AlreadyReviewed(OrderId orderId, UserId reviewerId)
            => Error.Conflict("SellerReview.AlreadyReviewed",
                $"User {reviewerId} has already submitted a review for order {orderId}.");

        public static Error NotBuyer(OrderId orderId)
            => Error.Forbidden("SellerReview.NotBuyer",
                $"Only the buyer of order {orderId} can submit a seller review.");
    }
}

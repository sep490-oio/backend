using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.OrderContext.Errors;

public static class OrderErrors
{
    public static class Order
    {
        public static Error NotFound(OrderId orderId) => Error.NotFound("Order.NotFound", $"Order with ID {orderId} not found.");
        public static Error InvalidState(string status, string action) 
            => Error.Validation(string.Empty, "Order.InvalidState", $"Cannot {action} because order is in {status} state.");

        public static Error PaymentFailed(OrderId orderId, string reason)
            => Error.Conflict("Order.PaymentFailed", $"Payment failed for order {orderId}. Reason: {reason}");
            
        public static Error CannotInitializePayment(OrderStatus status)
            => Error.Conflict("Order.CannotInitializePayment", $"Cannot initialize payment when order is {status}. Required: PendingPayment.");

        public static Error ReturnAlreadyExists(OrderId orderId)
            => Error.Conflict("Order.ReturnAlreadyExists", $"Order {orderId} already has an active return.");

        public static Error DecisionWindowNotStarted(OrderId orderId)
            => Error.Conflict("Order.DecisionWindowNotStarted", $"Decision window has not started for order {orderId}.");

        public static Error DecisionWindowExpired(OrderId orderId)
            => Error.Conflict("Order.DecisionWindowExpired", $"Decision window has expired for order {orderId}.");

        public static Error ReturnNotFound(OrderId orderId)
            => Error.NotFound("Order.ReturnNotFound", $"No return request found for order {orderId}.");

        public static Error EscrowNotFound(OrderId orderId)
            => Error.NotFound("Order.EscrowNotFound", $"No escrow found for order {orderId}.");
    }
}

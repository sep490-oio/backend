using CSharpFunctionalExtensions;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.OrderContext.Aggregates.Orders;

public sealed class OrderReturn : BaseEntity<OrderReturnId>
{
    public OrderId OrderId { get; private set; }
    public UserId BuyerId { get; private set; }
    public string ReasonCode { get; private set; }
    public string? Description { get; private set; }
    public OrderReturnStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? DecisionReason { get; private set; }
    public string? ProviderCode { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
    public DateTime? SellerReceivedAt { get; private set; }
    public DateTime? SellerConfirmedReceivedAt { get; private set; }
    public DateTime? BuyerDecisionDueAt { get; private set; }

    public Order Order { get; private set; } = null!;

    private OrderReturn() { }

    public static OrderReturn Create(
        OrderId orderId,
        UserId buyerId,
        string reasonCode,
        string? description,
        DateTime buyerDecisionDueAt,
        DateTime nowUtc)
    {
        return new OrderReturn
        {
            Id = OrderReturnId.From(Guid.CreateVersion7()),
            OrderId = orderId,
            BuyerId = buyerId,
            ReasonCode = reasonCode,
            Description = description,
            Status = OrderReturnStatus.Requested,
            RequestedAt = nowUtc,
            BuyerDecisionDueAt = buyerDecisionDueAt
        };
    }

    public UnitResult<Error> Approve(string? reason, DateTime nowUtc)
    {
        if (Status != OrderReturnStatus.Requested)
            return Error.Conflict("OrderReturn.InvalidState", "Return request cannot be approved.");

        Status = OrderReturnStatus.Approved;
        ApprovedAt = nowUtc;
        DecisionReason = reason;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject(string reason, DateTime nowUtc)
    {
        if (Status != OrderReturnStatus.Requested)
            return Error.Conflict("OrderReturn.InvalidState", "Return request cannot be rejected.");

        Status = OrderReturnStatus.Rejected;
        RejectedAt = nowUtc;
        DecisionReason = reason;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkReturnShipped(
        string providerCode,
        string trackingNumber,
        DateTime shippedAt,
        DateTime nowUtc)
    {
        if (Status != OrderReturnStatus.Approved)
            return Error.Conflict("OrderReturn.InvalidState", "Return shipment can only be created after approval.");

        ProviderCode = providerCode;
        TrackingNumber = trackingNumber;
        ShippedAt = shippedAt;
        Status = OrderReturnStatus.ReturnInTransit;
        ReturnedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkSellerReceived(DateTime nowUtc)
    {
        if (Status != OrderReturnStatus.ReturnInTransit && Status != OrderReturnStatus.Approved)
            return Error.Conflict("OrderReturn.InvalidState", "Seller can only confirm a return after the return is approved and in transit.");

        Status = OrderReturnStatus.SellerReceived;
        SellerReceivedAt = nowUtc;
        SellerConfirmedReceivedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Resolve(DateTime nowUtc)
    {
        if (Status != OrderReturnStatus.SellerReceived && Status != OrderReturnStatus.Rejected)
            return Error.Conflict("OrderReturn.InvalidState", "Return can only be resolved after seller confirmation or rejection.");

        Status = OrderReturnStatus.Resolved;
        ReturnedAt ??= nowUtc;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Cancel(string? reason, DateTime nowUtc)
    {
        if (Status == OrderReturnStatus.Resolved || Status == OrderReturnStatus.Cancelled)
            return Error.Conflict("OrderReturn.InvalidState", "Return is already terminal.");

        Status = OrderReturnStatus.Cancelled;
        DecisionReason = reason;
        ReturnedAt ??= nowUtc;
        return UnitResult.Success<Error>();
    }
}

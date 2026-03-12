using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

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
    public DateTime? SellerConfirmedReceivedAt { get; private set; }
    public DateTime? BuyerDecisionDueAt { get; private set; }

    // Navigation
    public Order Order { get; private set; } = null!;

    private OrderReturn() { }
}
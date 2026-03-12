using OIO.Domain.Context.ModerationContext.ValueObjects;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Invoices;

public sealed class Invoice : AggregateRoot<InvoiceId>
{
    public InvoiceNumber InvoiceNumber { get; private set; }
    public OrderId OrderId { get; private set; }
    public UserId BuyerId { get; private set; }
    public UserId SellerId { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public Money TotalAmount { get; private set; }
    public string Currency { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private Invoice() { }
}
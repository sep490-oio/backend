using OIO.Domain.Context.ModerationContext.ValueObjects;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.DomainEvents;
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

    public static Invoice Create(
        InvoiceNumber invoiceNumber,
        OrderId orderId,
        UserId buyerId,
        UserId sellerId,
        decimal subtotal,
        decimal taxAmount,
        Money totalAmount,
        string currency,
        DateTime nowUtc)
    {
        return new Invoice
        {
            Id = InvoiceId.From(Guid.CreateVersion7()),
            InvoiceNumber = invoiceNumber,
            OrderId = orderId,
            BuyerId = buyerId,
            SellerId = sellerId,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Currency = currency,
            Status = InvoiceStatus.Draft,
            IssuedAt = nowUtc
        };
    }

    public void MarkAsIssued(DateOnly dueDate, DateTime nowUtc)
    {
        if (Status != InvoiceStatus.Draft) return;

        Status = InvoiceStatus.Issued;
        DueDate = dueDate;
        IssuedAt = nowUtc;
    }

    public void MarkAsPaid(DateTime nowUtc)
    {
        if (Status == InvoiceStatus.Paid || Status == InvoiceStatus.Cancelled) return;

        Status = InvoiceStatus.Paid;
        PaidAt = nowUtc;

        RaiseDomainEvent(new InvoicePaidDomainEvent(
            Id, OrderId.Value, BuyerId, TotalAmount.Amount, nowUtc));
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Paid || Status == InvoiceStatus.Cancelled) return;

        Status = InvoiceStatus.Cancelled;
    }
}
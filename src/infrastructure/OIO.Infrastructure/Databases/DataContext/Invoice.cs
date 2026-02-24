using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Invoice
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public Guid OrderId { get; set; }

    public Guid BuyerId { get; set; }

    public Guid SellerId { get; set; }

    public decimal Subtotal { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Currency { get; set; }

    public string? Status { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual User Buyer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual User Seller { get; set; } = null!;
}

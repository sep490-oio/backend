using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class InvoiceStatus : EnumValueObject<InvoiceStatus>
{
    public static readonly InvoiceStatus Draft = new("draft");
    public static readonly InvoiceStatus Issued = new("issued");
    public static readonly InvoiceStatus Paid = new("paid");
    public static readonly InvoiceStatus Cancelled = new("cancelled");
    private InvoiceStatus(string id) : base(id) { }
}
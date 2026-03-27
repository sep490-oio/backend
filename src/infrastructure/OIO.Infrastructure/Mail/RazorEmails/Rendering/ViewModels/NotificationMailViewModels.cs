namespace OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;

public sealed class GenericNotificationMailViewModel
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? UserName { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionLabel { get; set; }
}

public sealed class OrderNotificationMailViewModel
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? UserName { get; set; }
    public string? ItemName { get; set; }
    public string? OrderStatus { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? ActionUrl { get; set; }
}

public sealed class PaymentNotificationMailViewModel
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? UserName { get; set; }
    public string? TransactionType { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public string? ActionUrl { get; set; }
}

public sealed class DisputeNotificationMailViewModel
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? UserName { get; set; }
    public string? DisputeTitle { get; set; }
    public string? Status { get; set; }
    public string? Resolution { get; set; }
    public string? ActionUrl { get; set; }
}

public sealed class WarehouseNotificationMailViewModel
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? UserName { get; set; }
    public string? ShipmentReference { get; set; }
    public string? Status { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ActionUrl { get; set; }
}

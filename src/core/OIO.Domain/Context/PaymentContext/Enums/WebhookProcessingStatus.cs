using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class WebhookProcessingStatus : EnumValueObject<WebhookProcessingStatus>
{
    public static readonly WebhookProcessingStatus Pending = new("pending");
    public static readonly WebhookProcessingStatus Processed = new("processed");
    public static readonly WebhookProcessingStatus Failed = new("failed");
    public static readonly WebhookProcessingStatus Ignored = new("ignored");

    private WebhookProcessingStatus(string id) : base(id) { }
}

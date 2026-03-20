using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Webhooks;

public sealed class GatewayWebhookEvent : AggregateRoot<GatewayWebhookEventId>, ICreatedAtEntity
{
    public string Provider { get; private set; }
    public string EventType { get; private set; }
    public string RawContent { get; private set; }
    public WebhookProcessingStatus ProcessingStatus { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private GatewayWebhookEvent() { }

    public static GatewayWebhookEvent Create(
        string provider,
        string eventType,
        string rawContent,
        DateTime nowUtc)
    {
        return new GatewayWebhookEvent
        {
            Id = GatewayWebhookEventId.From(Guid.CreateVersion7()),
            Provider = provider,
            EventType = eventType,
            RawContent = rawContent,
            ProcessingStatus = WebhookProcessingStatus.Pending,
            RetryCount = 0,
            NextRetryAt = nowUtc,
            CreatedAt = nowUtc
        };
    }

    public void MarkAsProcessed(DateTime nowUtc)
    {
        ProcessingStatus = WebhookProcessingStatus.Processed;
        ProcessedAt = nowUtc;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage, DateTime nowUtc)
    {
        ProcessingStatus = WebhookProcessingStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessedAt = nowUtc;
    }

    public void MarkAsPendingForRetry(string errorMessage, DateTime nextRetryAt)
    {
        ProcessingStatus = WebhookProcessingStatus.Pending;
        ErrorMessage = errorMessage;
        RetryCount++;
        NextRetryAt = nextRetryAt;
    }
}

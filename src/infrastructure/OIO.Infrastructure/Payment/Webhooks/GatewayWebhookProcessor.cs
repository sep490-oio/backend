using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.PaymentContext.Commands.ProcessVnPayCallback;
using OIO.Domain.Context.PaymentContext.Aggregates.Webhooks;
using OIO.Domain.Context.PaymentContext.Enums;

namespace OIO.Infrastructure.Payment.Webhooks;

internal sealed class GatewayWebhookProcessor
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;
    private readonly IClock _clock;
    private readonly ILogger<GatewayWebhookProcessor> _logger;

    public GatewayWebhookProcessor(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ISender sender,
        IClock clock,
        ILogger<GatewayWebhookProcessor> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _sender = sender;
        _clock = clock;
        _logger = logger;
    }

    public async Task ProcessAsync(int batchSize = 20, CancellationToken cancellationToken = default)
    {
        var nowUser = _clock.UtcNow;

        // Lấy danh sách webhook đang chờ xử lý và đến giờ retry
        var pendingEvents = await _dbContext.Set<GatewayWebhookEvent>()
            .Where(e => e.ProcessingStatus == WebhookProcessingStatus.Pending && 
                        (e.NextRetryAt == null || e.NextRetryAt <= nowUser))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pendingEvents.Count == 0) return;

        foreach (var webhookEvent in pendingEvents)
        {
            try
            {
                if (webhookEvent.Provider == "vnpay" && webhookEvent.EventType == "ipn")
                {
                    var queryParams = JsonSerializer.Deserialize<Dictionary<string, string>>(webhookEvent.RawContent)
                                      ?? new Dictionary<string, string>();

                    var command = new ProcessVnPayCallbackCommand(queryParams);
                    var result = await _sender.Send(command, cancellationToken);

                    if (result.IsSuccess)
                    {
                        webhookEvent.MarkAsProcessed(nowUser);
                    }
                    else
                    {
                        HandleWebhookFailure(webhookEvent, result.Error.Message, nowUser);
                    }
                }
                else
                {
                    // Provider khác hoặc event type khác => ignore
                    webhookEvent.MarkAsFailed($"Unknown provider/event: {webhookEvent.Provider}/{webhookEvent.EventType}", nowUser);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while processing webhook ID {WebhookId}", webhookEvent.Id.Value);
                HandleWebhookFailure(webhookEvent, $"Exception: {ex.Message}", nowUser);
            }

            _dbContext.Update(webhookEvent);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void HandleWebhookFailure(GatewayWebhookEvent webhookEvent, string errorMessage, DateTime nowUtc)
    {
        const int MaxRetries = 3;
        
        if (webhookEvent.RetryCount >= MaxRetries)
        {
            webhookEvent.MarkAsFailed(errorMessage, nowUtc);
            _logger.LogWarning("Webhook ID {WebhookId} permanently failed after {MaxRetries} retries. Error: {Error}", 
                webhookEvent.Id.Value, MaxRetries, errorMessage);
        }
        else
        {
            // Exponential backoff: 1m, 5m, 15m
            int currentRetry = webhookEvent.RetryCount;
            int delayMinutes = currentRetry switch
            {
                0 => 1,
                1 => 5,
                _ => 15
            };
            
            DateTime nextRetryAt = nowUtc.AddMinutes(delayMinutes);
            webhookEvent.MarkAsPendingForRetry(errorMessage, nextRetryAt);
            
            _logger.LogInformation("Webhook ID {WebhookId} failed attempt {Attempt}. Retrying at {NextRetryAt}. Error: {Error}", 
                webhookEvent.Id.Value, currentRetry + 1, nextRetryAt, errorMessage);
        }
    }
}

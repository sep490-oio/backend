using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Infrastructure.Mail.RazorEmails.Rendering;
using OIO.Infrastructure.Mail.RazorEmails.Rendering.ViewModels;
using OIO.Infrastructure.Mail.RazorEmails.Rendering.Views;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Notification.Providers;

internal sealed class EmailNotificationProvider : INotificationProvider
{
    private readonly IMailSender _mailSender;
    private readonly ApplicationDbContext _dbContext;
    private readonly RazorViewRenderer _renderer;
    private readonly ILogger<EmailNotificationProvider> _logger;

    public EmailNotificationProvider(
        IMailSender mailSender,
        ApplicationDbContext dbContext,
        RazorViewRenderer renderer,
        ILogger<EmailNotificationProvider> logger)
    {
        _mailSender = mailSender;
        _dbContext = dbContext;
        _renderer = renderer;
        _logger = logger;
    }

    public string ChannelType => "Email";

    public async Task<Result> SendAsync(
        OIO.Domain.Context.NotificationContext.Aggregates.Notification notification,
        NotificationDelivery delivery,
        CancellationToken ct = default)
    {
        var user = await _dbContext.Set<OIO.Domain.Context.UserContext.Aggregates.Users.User>()
            .FirstOrDefaultAsync(u => u.Id == notification.UserId, ct);

        if (user == null)
            return Result.Failure("User not found");

        try
        {
            var metadata = ParseMetadata(notification.Metadata);
            var userName = user.UserName.Value;
            var actionUrl = metadata.GetValueOrDefault("actionUrl");

            var htmlBody = await RenderTemplateAsync(notification, userName, actionUrl, metadata);

            var content = new MailContent(
                user.Email,
                $"OIO: {notification.Title}",
                htmlBody,
                notification.Message // plain text fallback
            );
            var success = await _mailSender.SendAsync(content, ct);
            return success ? Result.Success() : Result.Failure("MailSender returned false");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render email template for notification {NotificationId}, falling back to generic",
                notification.Id);
            return Result.Failure(ex.Message);
        }
    }

    private async Task<string> RenderTemplateAsync(
        OIO.Domain.Context.NotificationContext.Aggregates.Notification notification,
        string userName,
        string? actionUrl,
        Dictionary<string, string?> metadata)
    {
        var eventType = notification.EventType ?? "";

        try
        {
            if (eventType.StartsWith("order_", StringComparison.OrdinalIgnoreCase))
            {
                return await _renderer.Render<OrderNotificationMail, OrderNotificationMailViewModel>(
                    new OrderNotificationMailViewModel
                    {
                        Title = notification.Title,
                        Message = notification.Message,
                        UserName = userName,
                        ItemName = metadata.GetValueOrDefault("itemName"),
                        OrderStatus = metadata.GetValueOrDefault("status"),
                        Amount = TryParseDecimal(metadata.GetValueOrDefault("amount")),
                        Currency = metadata.GetValueOrDefault("currency"),
                        ActionUrl = actionUrl,
                    });
            }

            if (eventType.StartsWith("payment_", StringComparison.OrdinalIgnoreCase) ||
                eventType.StartsWith("deposit_", StringComparison.OrdinalIgnoreCase) ||
                eventType.StartsWith("withdrawal_", StringComparison.OrdinalIgnoreCase) ||
                eventType.StartsWith("escrow_", StringComparison.OrdinalIgnoreCase))
            {
                return await _renderer.Render<PaymentNotificationMail, PaymentNotificationMailViewModel>(
                    new PaymentNotificationMailViewModel
                    {
                        Title = notification.Title,
                        Message = notification.Message,
                        UserName = userName,
                        TransactionType = metadata.GetValueOrDefault("transactionType"),
                        Amount = TryParseDecimal(metadata.GetValueOrDefault("amount")),
                        Currency = metadata.GetValueOrDefault("currency"),
                        Status = metadata.GetValueOrDefault("status"),
                        ActionUrl = actionUrl,
                    });
            }

            if (eventType.StartsWith("dispute_", StringComparison.OrdinalIgnoreCase))
            {
                return await _renderer.Render<DisputeNotificationMail, DisputeNotificationMailViewModel>(
                    new DisputeNotificationMailViewModel
                    {
                        Title = notification.Title,
                        Message = notification.Message,
                        UserName = userName,
                        DisputeTitle = metadata.GetValueOrDefault("disputeTitle"),
                        Status = metadata.GetValueOrDefault("status"),
                        Resolution = metadata.GetValueOrDefault("resolution"),
                        ActionUrl = actionUrl,
                    });
            }

            if (eventType.StartsWith("inbound_", StringComparison.OrdinalIgnoreCase) ||
                eventType.StartsWith("outbound_", StringComparison.OrdinalIgnoreCase) ||
                eventType.StartsWith("warehouse_", StringComparison.OrdinalIgnoreCase))
            {
                return await _renderer.Render<WarehouseNotificationMail, WarehouseNotificationMailViewModel>(
                    new WarehouseNotificationMailViewModel
                    {
                        Title = notification.Title,
                        Message = notification.Message,
                        UserName = userName,
                        ShipmentReference = metadata.GetValueOrDefault("shipmentReference"),
                        Status = metadata.GetValueOrDefault("status"),
                        TrackingNumber = metadata.GetValueOrDefault("trackingNumber"),
                        ActionUrl = actionUrl,
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render type-specific template for EventType '{EventType}', falling back to generic", eventType);
        }

        // Fallback: generic branded template
        return await _renderer.Render<GenericNotificationMail, GenericNotificationMailViewModel>(
            new GenericNotificationMailViewModel
            {
                Title = notification.Title,
                Message = notification.Message,
                UserName = userName,
                ActionUrl = actionUrl,
                ActionLabel = metadata.GetValueOrDefault("actionLabel"),
            });
    }

    private static Dictionary<string, string?> ParseMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string?>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch
        {
            return new Dictionary<string, string?>();
        }
    }

    private static decimal? TryParseDecimal(string? value)
        => decimal.TryParse(value, out var result) ? result : null;
}

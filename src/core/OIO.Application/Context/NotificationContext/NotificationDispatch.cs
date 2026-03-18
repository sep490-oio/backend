using System.Globalization;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;

namespace OIO.Application.Context.NotificationContext;

internal static class NotificationDispatch
{
    private static readonly CultureInfo AmountCulture = CultureInfo.GetCultureInfo("vi-VN");

    public static string FormatAmount(decimal amount, string? currency = null)
    {
        var formatted = amount.ToString("N0", AmountCulture);
        return string.IsNullOrWhiteSpace(currency) ? formatted : $"{formatted} {currency}";
    }

    public static string? SerializeMetadata(object? metadata)
    {
        return metadata is null ? null : JsonSerializer.Serialize(metadata);
    }

    public static async Task DispatchAsync(
        ISender sender,
        ILogger logger,
        CreateNotificationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to create notification for user {UserId}. Event={EventType}. Error={Error}",
                command.UserId,
                command.EventType,
                result.Error.Message);
        }
    }
}

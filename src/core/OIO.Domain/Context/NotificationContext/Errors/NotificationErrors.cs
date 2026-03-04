using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.NotificationContext.Errors;

public static class NotificationErrors
{
    public static class Notification
    {
        public static readonly Func<NotificationId, Error> NotFound = id => Error.NotFound(
            code: "Notification.NotFound",
            description: $"Notification with id '{id}' was not found.");

        public static readonly Error NotificationDeleted = Error.Forbidden(
            code: "Notification.Deleted",
            description: "This notification has been deleted and cannot be modified.");

        public static readonly Error AlreadyRead = Error.Conflict(
            code: "Notification.AlreadyRead",
            description: "This notification has already been read.");

        public static readonly Error Expired = Error.Forbidden(
            code: "Notification.Expired",
            description: "This notification has expired.");
    }

    public static class Delivery
    {
        public static readonly Func<NotificationDeliveryId, Error> DeliveryNotFound = id => Error.NotFound(
            code: "Notification.Delivery.NotFound",
            description: $"Delivery with id '{id}' was not found on this notification.");

        public static readonly Error AlreadyDelivered = Error.Conflict(
            code: "Notification.Delivery.AlreadyDelivered",
            description: "This delivery has already been successfully delivered.");

        public static readonly Error MaxAttemptsExceeded = Error.Forbidden(
            code: "Notification.Delivery.MaxAttemptsExceeded",
            description: "Maximum delivery attempts have been exceeded.");

        public static readonly Error Cancelled = Error.Forbidden(
            code: "Notification.Delivery.Cancelled",
            description: "This delivery has been cancelled.");
    }

    public static class Preference
    {
        public static readonly Func<string, Error> NotFound = userId => Error.NotFound(
            code: "Notification.Preference.NotFound",
            description: $"Notification preferences for user '{userId}' were not found.");

        public static readonly Error InvalidQuietHours = Error.Validation(
            propertyName: "QuietHours",
            code: "Notification.Preference.QuietHours.Invalid",
            description: "Quiet hours configuration is invalid.");

        public static readonly Error InvalidRateLimits = Error.Validation(
            propertyName: "RateLimits",
            code: "Notification.Preference.RateLimits.Invalid",
            description: "Rate limit configuration is invalid.");
    }
}

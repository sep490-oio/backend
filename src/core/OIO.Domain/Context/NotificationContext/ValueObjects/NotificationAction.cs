using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Represents a single actionable button/link attached to a notification.
/// e.g. { "label": "View Order", "action": "navigate", "url": "/orders/123" }
/// </summary>
public sealed class NotificationAction : ValueObject
{
    private NotificationAction() { }

    private NotificationAction(string label, string action, string? url)
    {
        Label = label;
        Action = action;
        Url = url;
    }

    public string Label { get; private set; }
    public string Action { get; private set; }
    public string? Url { get; private set; }

    public static Result<NotificationAction, Error> Create(
        string label,
        string action,
        string? url = null)
    {
        var result = NotificationAction.Check(isInvariant: true)
            .Field(label, x => x.Label)!
            .NotNullOrWhiteSpace()
            .MaxLength(100)
            .Field(action, x => x.Action)!
            .NotNullOrWhiteSpace()
            .MaxLength(50)
            .ToResult();

        if (result.IsFailure)
            return result.Error;

        return new NotificationAction(label.Trim(), action.Trim(), url?.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Label;
        yield return Action;
        yield return Url;
    }
}

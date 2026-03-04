using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Groups <c>error_code</c>, <c>error_message</c>, and <c>error_details</c> columns
/// on <c>notification_delivery</c> into a cohesive value object.
/// EF maps each property individually via ComplexProperty.
/// A delivery that has never failed will have this as null on the entity.
/// </summary>
public sealed class DeliveryError : ValueObject
{
    private DeliveryError() { }

    private DeliveryError(string code, string message, string? detailsJson)
    {
        Code = code;
        Message = message;
        DetailsJson = detailsJson;
    }

    /// <summary>Short machine-readable error code, e.g. "SMTP_TIMEOUT", "FCM_INVALID_TOKEN".</summary>
    public string Code { get; private set; }

    /// <summary>Human-readable description of the error.</summary>
    public string Message { get; private set; }

    /// <summary>Optional JSON blob with provider-specific error details.</summary>
    public string? DetailsJson { get; private set; }

    public static Result<DeliveryError, OIO.Domain.SeedWork.Errors.Error> Create(
        string code,
        string message,
        string? detailsJson = null)
    {
        var result = DeliveryError.Check(isInvariant: true)
            .Field(code, x => x.Code)!
            .NotNullOrWhiteSpace()
            .MaxLength(50)
            .Field(message, x => x.Message)!
            .NotNullOrWhiteSpace()
            .ToResult();

        if (result.IsFailure)
            return result.Error;

        return new DeliveryError(code.Trim(), message.Trim(), detailsJson);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Message;
        yield return DetailsJson;
    }
}

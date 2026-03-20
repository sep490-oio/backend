namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record EscrowReleaseEventDto(
    Guid Id,
    string ReleaseType,
    string TriggerSourceType,
    Guid? TriggerSourceId,
    decimal Amount,
    Guid? CreatedBy,
    DateTime CreatedAt);

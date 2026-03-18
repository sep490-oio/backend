using OIO.Application.Abstractions.Messaging;
namespace OIO.Application.Context.WarehouseContext.Commands.ProcessTrackingWebhook;

public sealed record ProcessTrackingWebhookCommand(
    string  ProviderCode,
    /// <summary>Our reference — INB-xxx for inbound, OUT-xxx for outbound.</summary>
    string? ClientOrderCode,
    /// <summary>Carrier's own tracking number — fallback when ClientOrderCode is absent.</summary>
    string? CarrierTrackingNumber,
    string  CarrierStatusRaw,
    string? CarrierStatusDesc,
    string  NormalizedStatusId,    // NormalizedTrackingStatus.Id e.g. "delivered"
    string? Location,
    string? ReasonCode,
    string? ReasonDescription,
    DateTime EventTime,
    string  RawPayloadJson
) : ICommand;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Shipping;

public interface IShippingProvider
{
    /// <summary>"ghn" | "ghtk"</summary>
    string ProviderCode { get; }

    /// <summary>
    /// Books a shipment with the carrier and returns tracking number + label URL.
    /// Called for both inbound (seller → warehouse) and outbound (warehouse → buyer).
    /// </summary>
    Task<Result<CreateShipmentResponse, Error>> CreateShipmentAsync(
        CreateShipmentRequest    request,
        ShippingProviderConfig   config,
        CancellationToken        ct = default);

    /// <summary>Cancels a booked shipment. Only valid before pickup.</summary>
    Task<Result<Unit, Error>> CancelShipmentAsync(
        string                 carrierTrackingNumber,
        ShippingProviderConfig config,
        CancellationToken      ct = default);

    /// <summary>Calculates shipping fee before booking.</summary>
    Task<Result<decimal, Error>> CalculateFeeAsync(
        CalculateFeeRequest    request,
        ShippingProviderConfig config,
        CancellationToken      ct = default);

    /// <summary>
    /// Parses and verifies an incoming webhook payload.
    /// GHN:  verifies ShopId in payload matches config.
    /// GHTK: verifies ?hash= query param.
    /// Returns failure if verification fails.
    /// </summary>
    Result<ParsedWebhookEvent, Error> ParseWebhook(
        string                 body,
        IHeaderDictionary      headers,
        IQueryCollection       query,
        ShippingProviderConfig config);
}
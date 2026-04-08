using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.Commands.SellerDirectShipment;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.SellerDirectShipment;

namespace OIO.Api.Endpoints.OrderContext.SellerDirectShipment;

/// <summary>
/// POST /api/orders/{orderId}/self-shipments — seller creates the 1:1 direct
/// shipment record for a self-ship order. Returns 409 if a row already exists
/// or the order is warehouse-managed.
/// </summary>
public sealed class CreateSellerDirectShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.CreateSellerDirectShipment, async (
                Guid orderId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateSellerDirectShipmentCommand(orderId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.CreateSellerDirectShipment)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

public sealed record SetSellerDirectShipmentCarrierInfoBody(string ExternalCarrierName, string ExternalTrackingCode);

public sealed class SetSellerDirectShipmentCarrierInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Orders.SetSellerDirectShipmentCarrierInfo, async (
                Guid orderId,
                Guid shipmentId,
                SetSellerDirectShipmentCarrierInfoBody body,
                ISender sender,
                CancellationToken ct) =>
            {
                _ = orderId;
                var result = await sender.Send(
                    new SetSellerDirectShipmentCarrierInfoCommand(shipmentId, body.ExternalCarrierName, body.ExternalTrackingCode),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.SetSellerDirectShipmentCarrierInfo)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

public sealed class MarkSellerDirectShipmentPickedUpEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.MarkSellerDirectShipmentPickedUp, async (
                Guid orderId,
                Guid shipmentId,
                ISender sender,
                CancellationToken ct) =>
            {
                _ = orderId;
                var result = await sender.Send(new MarkSellerDirectShipmentPickedUpCommand(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.MarkSellerDirectShipmentPickedUp)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

public sealed class MarkSellerDirectShipmentOnDeliveringEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.MarkSellerDirectShipmentOnDelivering, async (
                Guid orderId,
                Guid shipmentId,
                ISender sender,
                CancellationToken ct) =>
            {
                _ = orderId;
                var result = await sender.Send(new MarkSellerDirectShipmentOnDeliveringCommand(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.MarkSellerDirectShipmentOnDelivering)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

public sealed class MarkSellerDirectShipmentDeliveredEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.MarkSellerDirectShipmentDelivered, async (
                Guid orderId,
                Guid shipmentId,
                ISender sender,
                CancellationToken ct) =>
            {
                _ = orderId;
                var result = await sender.Send(new MarkSellerDirectShipmentDeliveredCommand(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.MarkSellerDirectShipmentDelivered)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>GET /api/me/shipments/{shipmentId} — buyer deep-link landing.</summary>
public sealed class GetMyDirectShipmentByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyDirectShipmentById, async (
                Guid shipmentId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyDirectShipmentByIdQuery(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyDirectShipmentById)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

/// <summary>
/// GET /api/me/orders/seller-direct-ship/shipments/{shipmentId} — seller deep-link
/// landing for the direct shipment they own. Reuses <see cref="GetSellerDirectShipmentByIdQuery"/>
/// which enforces seller ownership (mismatches → 404).
/// </summary>
public sealed class GetSellerDirectShipmentByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetSellerDirectShipmentById, async (
                Guid shipmentId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSellerDirectShipmentByIdQuery(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetSellerDirectShipmentById)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

/// <summary>
/// GET /api/me/orders/seller-direct-ship/shipments — seller-safe paged list
/// of direct shipments the current seller owns. Enriched with product +
/// recipient context so the seller list UI renders without a second
/// round-trip.
/// </summary>
public sealed class GetSellerDirectShipmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetSellerDirectShipments, async (
                [AsParameters] PagedParameters parameters,
                string? status,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetSellerDirectShipmentsQuery(parameters, status), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetSellerDirectShipments)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<PagedList<SellerDirectShipmentListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

/// <summary>
/// GET /api/me/shipments — buyer-facing paged list of direct shipments
/// against the current user's orders. Includes seller display name,
/// decision-window deadline, and precomputed action flags so the buyer
/// list UI can gate CTAs without fetching individual orders.
/// </summary>
public sealed class GetMyDirectShipmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyDirectShipments, async (
                [AsParameters] PagedParameters parameters,
                string? status,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetMyDirectShipmentsQuery(parameters, status), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyDirectShipments)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<PagedList<MyDirectShipmentListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

public sealed record SetSellerDirectShipmentDispatchDetailsBody(
    string CarrierName,
    string TrackingNumber,
    DateTime ShippedAt,
    List<Guid> PackagePhotoMediaUploadIds);

public sealed class SetSellerDirectShipmentDispatchDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Orders.SetSellerDirectShipmentDispatchDetails, async (
                Guid orderId,
                Guid shipmentId,
                SetSellerDirectShipmentDispatchDetailsBody body,
                ISender sender,
                CancellationToken ct) =>
            {
                _ = orderId;
                var result = await sender.Send(
                    new SetSellerDirectShipmentDispatchDetailsCommand(
                        shipmentId,
                        body.CarrierName,
                        body.TrackingNumber,
                        body.ShippedAt,
                        body.PackagePhotoMediaUploadIds ?? new List<Guid>()),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.SetSellerDirectShipmentDispatchDetails)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

public sealed record AddSellerDirectShipmentHandoverProofsBody(
    List<Guid> HandoverProofMediaUploadIds);

public sealed class AddSellerDirectShipmentHandoverProofsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.AddSellerDirectShipmentHandoverProofs, async (
                Guid orderId,
                Guid shipmentId,
                AddSellerDirectShipmentHandoverProofsBody body,
                ISender sender,
                CancellationToken ct) =>
            {
                _ = orderId;
                var result = await sender.Send(
                    new AddSellerDirectShipmentHandoverProofsCommand(
                        shipmentId,
                        body.HandoverProofMediaUploadIds ?? new List<Guid>()),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.AddSellerDirectShipmentHandoverProofs)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

public sealed record SubmitProofOfDeliveryBody(
    List<Guid> DeliveryPhotoMediaUploadIds,
    string PackageCondition,
    string? ConditionNotes,
    string Source);

public sealed class SubmitProofOfDeliveryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.SubmitProofOfDelivery, async (
                Guid shipmentId,
                SubmitProofOfDeliveryBody body,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new SubmitProofOfDeliveryCommand(
                        shipmentId,
                        body.DeliveryPhotoMediaUploadIds ?? new List<Guid>(),
                        body.PackageCondition,
                        body.ConditionNotes,
                        body.Source),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.SubmitProofOfDelivery)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

public sealed record ValidateDirectShipmentScanBody(string Token);

public sealed class ValidateDirectShipmentScanEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.ValidateDirectShipmentScan, async (
                ValidateDirectShipmentScanBody body,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ValidateSellerDirectShipmentScanCommand(body.Token),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.ValidateDirectShipmentScan)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<ValidateSellerDirectShipmentScanResultDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

/// <summary>POST /api/me/shipments/{shipmentId}/acknowledge-received — buyer stamp.</summary>
public sealed class AcknowledgeDirectShipmentReceivedEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.AcknowledgeDirectShipmentReceived, async (
                Guid shipmentId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new BuyerAcknowledgeDirectShipmentReceivedCommand(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.AcknowledgeDirectShipmentReceived)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<SellerDirectShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

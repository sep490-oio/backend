using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.SetExternalTrackingNumber;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class SetExternalTrackingNumberEndpoint : IEndpoint
{
    public sealed record Request(string TrackingNumber);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Warehouse.SetExternalTracking, async (
                Guid shipmentId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SetExternalTrackingNumberCommand(shipmentId, request.TrackingNumber);
                var result  = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookInbound)
            .WithName(ApiEndpoint.Names.Warehouse.SetExternalTrackingNumber)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<InboundShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
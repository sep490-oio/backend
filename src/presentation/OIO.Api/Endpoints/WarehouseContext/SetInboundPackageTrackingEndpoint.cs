using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.SetInboundPackageTracking;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class SetInboundPackageTrackingEndpoint : IEndpoint
{
    public sealed record Request(string TrackingNumber);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Warehouse.SetInboundPackageTracking, async (
                string clientOrderCode,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new SetInboundPackageTrackingCommand(clientOrderCode, request.TrackingNumber), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookInbound)
            .WithName(ApiEndpoint.Names.Warehouse.SetInboundPackageTracking)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.CancelInboundPackage;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class CancelInboundPackageEndpoint : IEndpoint
{
    public sealed record Request(string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.CancelInboundPackage, async (
                string clientOrderCode,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CancelInboundPackageCommand(clientOrderCode, request.Reason ?? string.Empty), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.CancelInboundPackage)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

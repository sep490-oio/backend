using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.UpdateInboundPackageExternalStatus;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class UpdateInboundPackageExternalStatusEndpoint : IEndpoint
{
    public sealed record Request(string Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Warehouse.UpdateInboundPackageStatus, async (
                string clientOrderCode,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UpdateInboundPackageExternalStatusCommand(clientOrderCode, request.Status), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.UpdateExternalStatus)
            .WithName(ApiEndpoint.Names.Warehouse.UpdateInboundPackageStatus)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

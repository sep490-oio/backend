using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.RequestWarehouseReinspection;
using OIO.Application.Extensions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext.Sellers;

/// <summary>
/// POST /api/seller/warehouse/items/{warehouseItemId}/request-reinspection
///
/// Seller-driven re-inspection request for a rejected warehouse item that is
/// still physically inside the warehouse. Online-review resubmit is blocked
/// for warehouse-bound items via <c>ResubmitItemCommandHandler</c> — this is
/// the dedicated path back into the warehouse inspector queue.
/// </summary>
public sealed class RequestWarehouseReinspectionEndpoint : IEndpoint
{
    public sealed record Request(string? Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.SellerWarehouseRequestReinspection, async (
                Guid warehouseItemId,
                Request? request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new RequestWarehouseReinspectionCommand(warehouseItemId, request?.Reason),
                    ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.RequestWarehouseReinspection)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

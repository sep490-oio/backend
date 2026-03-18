using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.InspectWarehouseItem;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class InspectWarehouseItemEndpoint : IEndpoint
{
    public sealed record Request(
        string ConditionId,
        string? InspectionNotes,
        IReadOnlyList<Guid> InspectionMediaUploadIds);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.InspectWarehouseItem, async (
                Guid shipmentId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new InspectWarehouseItemCommand(
                    shipmentId,
                    request.ConditionId,
                    request.InspectionNotes,
                    request.InspectionMediaUploadIds);

                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.Inspect)
            .WithName(ApiEndpoint.Names.Warehouse.InspectWarehouseItem)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseInspectionDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

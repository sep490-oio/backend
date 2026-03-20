using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.InspectWarehouseItem;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class InspectWarehouseItemEndpoint : IEndpoint
{
    /// <summary>
    /// All 4 photo slots are optional individually.
    /// Upload IDs must be pre-confirmed via the media upload endpoint.
    /// </summary>
    public sealed record Request(
        [Required] string Condition,
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
                    request.Condition,
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

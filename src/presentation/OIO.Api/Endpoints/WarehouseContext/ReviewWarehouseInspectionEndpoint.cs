using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.ReviewWarehouseInspection;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class ReviewWarehouseInspectionEndpoint : IEndpoint
{
    public sealed record Request([Required] string Decision, string? Reason = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.ReviewWarehouseInspection, async (
                Guid shipmentId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ReviewWarehouseInspectionCommand(
                    shipmentId,
                    request.Decision,
                    request.Reason);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.Inspect)
            .WithName(ApiEndpoint.Names.Warehouse.ReviewWarehouseInspection)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseInspectionDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

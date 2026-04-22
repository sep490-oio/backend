using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.RetryPendingInspectionReject;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext.WarehouseReturns;

/// <summary>
/// POST /api/admin/warehouse-returns/retry/{inspectionId} — admin recovers a
/// rejected inspection that previously failed to produce a shipment (most
/// commonly because the seller had no default address at the time).
/// </summary>
public sealed class RetryPendingInspectionRejectEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.RetryPendingInspectionReject, async (
                Guid inspectionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new RetryPendingInspectionRejectCommand(inspectionId), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.RetryPendingInspectionReject)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

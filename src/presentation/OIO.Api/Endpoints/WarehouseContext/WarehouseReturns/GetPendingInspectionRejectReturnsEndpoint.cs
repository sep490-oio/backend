using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetPendingInspectionRejectReturns;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext.WarehouseReturns;

/// <summary>
/// GET /api/admin/warehouse-returns/pending-inspection-rejects - rejected
/// inspections whose return-to-seller shipment was not created yet.
/// </summary>
public sealed class GetPendingInspectionRejectReturnsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.PendingInspectionRejectReturns, async (
                [AsParameters] PagedParameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPendingInspectionRejectReturnsQuery(parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.GetPendingInspectionRejectReturns)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<PagedList<PendingInspectionRejectReturnDto>>(StatusCodes.Status200OK);
    }
}

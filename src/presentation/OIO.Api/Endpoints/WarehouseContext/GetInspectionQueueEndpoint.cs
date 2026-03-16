using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetInspectionQueue;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetInspectionQueueEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.InspectionQueue, async (
                int page = 1,
                int pageSize = 20,
                ISender sender = default!,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(new GetInspectionQueueQuery(page, pageSize), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetInspectionQueue)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<IReadOnlyList<InspectionQueueItemDto>>(StatusCodes.Status200OK);
    }
}

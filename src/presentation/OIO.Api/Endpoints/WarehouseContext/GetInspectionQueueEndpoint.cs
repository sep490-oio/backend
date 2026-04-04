using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetInspectionQueue;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetInspectionQueueEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.InspectionQueue, async (
                int? pageNumber = null,
                int? pageSize = null,
                string? status = null,
                ISender sender = default!,
                CancellationToken ct = default) =>
            {
                var filters = new GetInspectionQueueQueryFilters
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Status = status,
                };
                var result = await sender.Send(new GetInspectionQueueQuery(filters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetInspectionQueue)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<InspectionQueueItemDto>>(StatusCodes.Status200OK);
    }
}

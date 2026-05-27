using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetInboundPackages;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetInboundPackagesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.InboundPackages, async (
                [AsParameters] GetInboundPackagesQueryFilter filter,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetInboundPackagesQuery(filter), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetInboundPackages)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<InboundPackageDto>>(StatusCodes.Status200OK);
    }
}

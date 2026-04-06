using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.WarehouseContext.Commands.AdjustWarehouseItem;
using OIO.Application.Extensions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class AdjustWarehouseItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.WarehouseItems + "/{id}/adjust", async (
                Guid id,
                AdjustWarehouseItemRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new AdjustWarehouseItemCommand(id, request.NewStatus, request.Reason), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.AdjustItem)
            .WithName(ApiEndpoint.Names.Warehouse.AdjustWarehouseItem)
            .WithTags(ApiEndpoint.Tags.Warehouse);
    }
}

public record AdjustWarehouseItemRequest(string NewStatus, string Reason);

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.WarehouseContext.Commands.MoveWarehouseItem;
using OIO.Application.Extensions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class MoveWarehouseItemEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.WarehouseItems + "/{id}/move", async (
                Guid id,
                MoveWarehouseItemRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new MoveWarehouseItemCommand(id, request.NewLocationId), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.MoveItem)
            .WithTags(ApiEndpoint.Tags.Warehouse);
    }
}

public record MoveWarehouseItemRequest(Guid NewLocationId);

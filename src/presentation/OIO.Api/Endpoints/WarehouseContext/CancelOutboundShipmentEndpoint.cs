using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.WarehouseContext.Commands.CancelOutboundShipment;
using OIO.Application.Extensions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class CancelOutboundShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.OutboundShipmentById + "/cancel", async (
                Guid id,
                CancelOutboundShipmentRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new CancelOutboundShipmentCommand(id, request.Reason), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.CancelOutbound)
            .WithTags(ApiEndpoint.Tags.Warehouse);
    }
}

public record CancelOutboundShipmentRequest(string Reason);

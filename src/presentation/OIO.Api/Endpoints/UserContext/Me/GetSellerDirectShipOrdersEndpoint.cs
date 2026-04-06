using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.GetSellerDirectShipOrders;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetSellerDirectShipOrdersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetSellerDirectShipOrders, async (
                [AsParameters] PagedParameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSellerDirectShipOrdersQuery(parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadDirectShipOrders)
            .WithName(ApiEndpoint.Names.Me.GetSellerDirectShipOrders)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<PagedList<OrderDto>>(StatusCodes.Status200OK);
    }
}

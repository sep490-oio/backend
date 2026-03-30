using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.GetMyOrders;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyOrdersEndpoint : IEndpoint
{
    public sealed record Parameters : GetMyOrdersQueryFilter;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyOrders, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyOrdersQuery(parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyOrders)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<PagedList<OrderDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

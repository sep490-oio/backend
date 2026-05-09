using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.Admin.GetAdminOrders;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.OrderContext.Admins;

public sealed class GetAdminOrdersEndpoint : IEndpoint
{
    public sealed record Parameters : GetAdminOrdersQueryFilter;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetAdminOrders, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetAdminOrdersQuery(parameters),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPayments)
            .WithName(ApiEndpoint.Names.Admins.GetAdminOrders)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<PagedList<AdminOrderListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}

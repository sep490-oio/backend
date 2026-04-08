using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctionById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.OrderContext.Admins;

public sealed class GetCompletedAuctionByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetCompletedAuctionById, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetCompletedAuctionByIdQuery(auctionId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName(ApiEndpoint.Names.Admins.GetCompletedAuctionById)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<AdminCompletedAuctionDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

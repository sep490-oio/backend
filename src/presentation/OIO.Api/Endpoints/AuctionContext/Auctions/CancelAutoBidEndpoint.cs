using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.CancelAutoBid;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class CancelAutoBidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.CancelAutoBid, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CancelAutoBidCommand(auctionId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.AutoBid)
            .WithName(ApiEndpoint.Names.Auctions.CancelAutoBid)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

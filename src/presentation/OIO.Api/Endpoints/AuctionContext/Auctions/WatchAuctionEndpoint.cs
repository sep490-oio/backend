using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.WatchAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class WatchAuctionEndpoint : IEndpoint
{
    public sealed record Request(
        bool NotifyOnBid = true,
        bool NotifyOnEnd = true);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.Watch, async (
                Guid auctionId,
                Request? request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new WatchAuctionCommand(
                    auctionId,
                    request?.NotifyOnBid ?? true,
                    request?.NotifyOnEnd ?? true);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Auctions.WatchAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
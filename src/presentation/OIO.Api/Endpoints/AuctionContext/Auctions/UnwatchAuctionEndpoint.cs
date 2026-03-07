using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.UnwatchAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class UnwatchAuctionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Auctions.Unwatch, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UnwatchAuctionCommand(auctionId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Auctions.UnwatchAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent);
    }
}
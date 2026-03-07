using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.PauseAutoBid;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class PauseAutoBidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.PauseAutoBid, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new PauseAutoBidCommand(auctionId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Auctions.PauseAutoBid)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
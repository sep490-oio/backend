using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.ResumeAutoBid;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class ResumeAutoBidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.ResumeAutoBid, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ResumeAutoBidCommand(auctionId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.AutoBid)
            .WithName(ApiEndpoint.Names.Auctions.ResumeAutoBid)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
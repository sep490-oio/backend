using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.EndAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class CloseAuctionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.Close, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new EndAuctionCommand(auctionId), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Cancel)
            .WithName(ApiEndpoint.Names.Auctions.CloseAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

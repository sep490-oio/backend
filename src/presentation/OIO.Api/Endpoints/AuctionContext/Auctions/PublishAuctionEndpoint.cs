using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.PublishAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class PublishAuctionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.Publish, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new PublishAuctionCommand(auctionId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Publish)
            .WithName(ApiEndpoint.Names.Auctions.PublishAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.SubmitAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class SubmitAuctionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.Submit, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SubmitAuctionCommand(auctionId);
                var result = await sender.Send(command, ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Submit)
            .WithName(ApiEndpoint.Names.Auctions.SubmitAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

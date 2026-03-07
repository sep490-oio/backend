using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.SetPrimaryImage;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class SetPrimaryImageEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.SetPrimaryImage, async (
                Guid itemId,
                Guid mediaId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SetPrimaryImageCommand(itemId, mediaId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Items.SetPrimaryItemImage)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
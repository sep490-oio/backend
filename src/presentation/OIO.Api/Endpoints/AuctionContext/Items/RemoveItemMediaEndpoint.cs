using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.RemoveItemMedia;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class RemoveItemMediaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Items.RemoveMedia, async (
                Guid itemId,
                Guid mediaId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RemoveItemMediaCommand(itemId, mediaId);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.ManageMedia)
            .WithName(ApiEndpoint.Names.Items.RemoveItemMedia)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.UpdateWatcherPreferences;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class UpdateWatcherPreferencesEndpoint : IEndpoint
{
    public sealed record Request(bool? NotifyOnBid = null, bool? NotifyOnEnd = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Auctions.WatchPreferences, async (
                Guid auctionId,
                Request? request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateWatcherPreferencesCommand(
                    auctionId,
                    request?.NotifyOnBid,
                    request?.NotifyOnEnd);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Watch)
            .WithName(ApiEndpoint.Names.Auctions.UpdateWatcherPreferences)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.RepairStuckInAuctionItems;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class RepairStuckInAuctionItemsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.RepairStuckInAuctionItems, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RepairStuckInAuctionItemsCommand();
                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.RepairStuckInAuctionItems)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<RepairStuckInAuctionItemsResponse>(StatusCodes.Status200OK);
    }
}

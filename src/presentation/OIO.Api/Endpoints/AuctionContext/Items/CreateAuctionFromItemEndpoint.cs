using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.CreateAuctionFromItem;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class CreateAuctionFromItemEndpoint : IEndpoint
{
    public sealed record Request(
        decimal StartingPrice = 0,
        decimal BidIncrement = 0,
        decimal? ReservePrice = null,
        decimal? BuyNowPrice = null,
        int ExtensionMinutes = 5,
        string Currency = "VND",
        string AuctionType = "regular");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.CreateAuction, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CreateAuctionFromItemCommand(
                    ItemId: itemId,
                    StartingPrice: request.StartingPrice,
                    BidIncrement: request.BidIncrement,
                    ReservePrice: request.ReservePrice,
                    BuyNowPrice: request.BuyNowPrice,
                    ExtensionMinutes: request.ExtensionMinutes,
                    Currency: request.Currency,
                    AuctionType: request.AuctionType);

                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Create)
            .WithName(ApiEndpoint.Names.Items.CreateAuctionFromItem)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}

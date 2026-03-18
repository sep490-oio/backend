using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.CreateAuction;
using OIO.Application.Context.AuctionContext.Commands.CreateItem;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class CreateAuctionEndpoint : IEndpoint
{
    public sealed record Request(
        // Item info
        string Title,
        string Condition,
        Guid? CategoryId = null,
        string? Description = null,
        int Quantity = 1,
        string? Attributes = null,
        IReadOnlyList<MediaAttachment>? Media = null,
        // Auction pricing
        decimal StartingPrice = 0,
        decimal BidIncrement = 0,
        decimal? ReservePrice = null,
        decimal? BuyNowPrice = null,
        int ExtensionMinutes = 5,
        string Currency = "VND",
        string AuctionType = "regular");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.Create, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CreateAuctionCommand(
                    Title: request.Title,
                    Condition: request.Condition,
                    CategoryId: request.CategoryId,
                    Description: request.Description,
                    Quantity: request.Quantity,
                    Attributes: request.Attributes,
                    Media: request.Media,
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
            .WithName(ApiEndpoint.Names.Auctions.CreateAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}

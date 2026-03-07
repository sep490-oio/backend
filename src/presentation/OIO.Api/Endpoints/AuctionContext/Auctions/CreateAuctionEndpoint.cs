using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Context.AuctionContext.Commands.CreateAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class CreateAuctionEndpoint : IEndpoint
{
    public sealed record Request(
        Guid ItemId,
        decimal StartingPrice,
        decimal BidIncrement,
        DateTime StartTime,
        DateTime EndTime,
        decimal? ReservePrice = null,
        decimal? BuyNowPrice = null,
        bool AutoExtend = true,
        int ExtensionMinutes = 5,
        string Currency = "VND");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.Create, async (
                Request request,
                ISender sender,
                IClock clock,
                CancellationToken ct) =>
            {
                var nowUtc = clock.UtcNow;
                var command = new CreateAuctionCommand(
                    nowUtc,
                    request.ItemId,
                    request.StartingPrice,
                    request.BidIncrement,
                    request.StartTime,
                    request.EndTime,
                    request.ReservePrice,
                    request.BuyNowPrice,
                    request.AutoExtend,
                    request.ExtensionMinutes,
                    request.Currency);

                var result = await sender.Send(command, ct);

                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Auctions.CreateAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
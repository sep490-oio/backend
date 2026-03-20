using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.OfferRunnerUp;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class OfferRunnerUpEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.OfferRunnerUp, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new OfferRunnerUpCommand(auctionId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Submit)
            .WithName(ApiEndpoint.Names.Auctions.OfferRunnerUp)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces<WinnerOfferDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

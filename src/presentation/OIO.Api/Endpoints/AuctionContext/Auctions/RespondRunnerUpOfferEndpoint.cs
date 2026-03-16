using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.RespondRunnerUpOffer;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class RespondRunnerUpOfferEndpoint : IEndpoint
{
    public sealed record Request(bool Accept);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.RespondRunnerUpOffer, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new RespondRunnerUpOfferCommand(auctionId, request.Accept),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Bid)
            .WithName(ApiEndpoint.Names.Auctions.RespondRunnerUpOffer)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces<WinnerOfferDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

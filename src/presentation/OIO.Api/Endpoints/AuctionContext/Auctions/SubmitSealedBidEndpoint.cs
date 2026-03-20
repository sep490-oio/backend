using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.SubmitSealedBid;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class SubmitSealedBidEndpoint : IEndpoint
{
    public sealed record Request([Required] decimal Amount);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.SubmitSealedBid, async (
                Guid auctionId,
                Request request,
                ISealedBidEncryptionService sealedBidEncryptionService,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new SubmitSealedBidCommand(
                        auctionId,
                        sealedBidEncryptionService.Encrypt(request.Amount)),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Bid)
            .WithName(ApiEndpoint.Names.Auctions.SubmitSealedBid)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}

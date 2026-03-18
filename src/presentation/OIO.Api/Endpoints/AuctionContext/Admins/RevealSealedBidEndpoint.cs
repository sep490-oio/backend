using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.AuctionContext.Commands.AdminRevealSealedBid;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class RevealSealedBidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.RevealSealedBid, async (
                Guid auctionId,
                Guid sealedBidId,
                ISealedBidEncryptionService sealedBidEncryptionService,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AdminRevealSealedBidCommand(auctionId, sealedBidId),
                    ct);

                if (result.IsFailure)
                    return result.Error.ToProblemDetails();

                var revealedAmount = sealedBidEncryptionService.Decrypt(result.Value.AmountEncrypted);

                if (revealedAmount.IsFailure)
                    return revealedAmount.Error.ToProblemDetails();

                return Results.Ok(result.Value with { RevealedAmount = revealedAmount.Value });
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.RevealSealedBid)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

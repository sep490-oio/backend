using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.ModerationContext.Commands.CancelInvalidBid;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class CancelInvalidBidEndpoint : IEndpoint
{
    public sealed record Request(string? Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.CancelInvalidBid, async (
                Guid auctionId,
                Guid bidId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CancelInvalidBidCommand(auctionId, bidId, request.Reason),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.CancelInvalidBid)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<BidDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

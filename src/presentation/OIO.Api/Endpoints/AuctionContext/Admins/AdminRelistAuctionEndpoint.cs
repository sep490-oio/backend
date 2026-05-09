using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.Admin.AdminRelistAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class AdminRelistAuctionEndpoint : IEndpoint
{
    public sealed record Request(
        DateTime QualificationStartAt,
        DateTime QualificationEndAt,
        DateTime StartAt,
        DateTime EndAt,
        decimal? StartingPrice = null,
        decimal? BidIncrement = null,
        decimal? ReservePrice = null,
        decimal? BuyNowPrice = null,
        string? Currency = null,
        string? Reason = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AdminRelistAuction, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AdminRelistAuctionCommand(
                        auctionId,
                        request.QualificationStartAt,
                        request.QualificationEndAt,
                        request.StartAt,
                        request.EndAt,
                        request.StartingPrice,
                        request.BidIncrement,
                        request.ReservePrice,
                        request.BuyNowPrice,
                        request.Currency,
                        request.Reason),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.AdminRelistAuction)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.UpdateAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class UpdateAuctionEndpoint : IEndpoint
{
    public sealed record Request(
        decimal? StartingPrice = null,
        decimal? BidIncrement = null,
        decimal? ReservePrice = null,
        decimal? BuyNowPrice = null,
        string? Currency = null,
        string? AuctionType = null,
        DateTime? StartTime = null,
        DateTime? EndTime = null,
        DateTime? QualificationStartAt = null,
        DateTime? QualificationEndAt = null,
        bool? AutoExtend = null,
        int? ExtensionMinutes = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Auctions.Update, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateAuctionCommand(
                    AuctionId: auctionId,
                    StartingPrice: request.StartingPrice,
                    BidIncrement: request.BidIncrement,
                    ReservePrice: request.ReservePrice,
                    BuyNowPrice: request.BuyNowPrice,
                    Currency: request.Currency,
                    AuctionType: request.AuctionType,
                    StartTime: request.StartTime,
                    EndTime: request.EndTime,
                    QualificationStartAt: request.QualificationStartAt,
                    QualificationEndAt: request.QualificationEndAt,
                    AutoExtend: request.AutoExtend,
                    ExtensionMinutes: request.ExtensionMinutes);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Create)
            .WithName(ApiEndpoint.Names.Auctions.UpdateAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}

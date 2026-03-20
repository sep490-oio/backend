using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.RelistAuction;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class RelistAuctionEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] DateTime QualificationStartAt,
        [Required] DateTime QualificationEndAt,
        [Required] DateTime StartAt,
        [Required] DateTime EndAt,
        decimal? StartingPrice = null,
        decimal? BidIncrement = null,
        decimal? ReservePrice = null,
        decimal? BuyNowPrice = null,
        string? Currency = null,
        string? Reason = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.Relist, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new RelistAuctionCommand(
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
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Submit)
            .WithName(ApiEndpoint.Names.Auctions.RelistAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces<AuctionDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

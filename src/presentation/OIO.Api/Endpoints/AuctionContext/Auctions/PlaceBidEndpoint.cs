using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Api.Filters;
using OIO.Application.Context.AuctionContext.Commands.PlaceBid;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class PlaceBidEndpoint : IEndpoint
{
    public sealed record Request(decimal Amount, string Currency = "VND");

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.PlaceBid, async (
                Guid auctionId,
                Request request,
                ISender sender,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var command = new PlaceBidCommand(
                    auctionId,
                    request.Amount,
                    request.Currency,
                    httpContext.GetIpAddress());

                return await sender.Send(command, ct);
            })
            .AddEndpointFilter(new IdempotencyFilter<BidDto>(
                IdempotencyHttpPolicies.PlaceBid()))
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Bid)
            .WithName(ApiEndpoint.Names.Auctions.PlaceBid)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}

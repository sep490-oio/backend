using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Api.Filters;
using OIO.Application.Context.AuctionContext.Commands.BuyNow;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class BuyNowEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.BuyNow, async (
                Guid auctionId,
                ISender sender,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var command = new BuyNowCommand(auctionId, httpContext.GetIpAddress());
                var result = await sender.Send(command, ct);

                return result;
            })
            .AddEndpointFilter(new IdempotencyFilter<BuyNowReservationDto>(IdempotencyHttpPolicies.BuyNow()))
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.BuyNow)
            .WithName(ApiEndpoint.Names.Auctions.BuyNow)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

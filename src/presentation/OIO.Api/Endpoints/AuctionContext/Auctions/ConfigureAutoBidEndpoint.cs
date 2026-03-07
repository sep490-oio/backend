using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.ConfigureAutoBid;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class ConfigureAutoBidEndpoint : IEndpoint
{
    public sealed record Request(
        decimal MaxAmount,
        string Currency = "VND",
        decimal? IncrementAmount = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Auctions.ConfigureAutoBid, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ConfigureAutoBidCommand(
                    auctionId,
                    request.MaxAmount,
                    request.Currency,
                    request.IncrementAmount);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Auctions.ConfigureAutoBid)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}
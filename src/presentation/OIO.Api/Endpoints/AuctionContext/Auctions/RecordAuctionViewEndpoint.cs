using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.RecordAuctionView;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class RecordAuctionViewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.RecordView, async (
                Guid auctionId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken ct) =>
            {
                var browserViewerId = httpContext.Request.Headers["X-Viewer-Id"].FirstOrDefault();
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

                var command = new RecordAuctionViewCommand(
                    auctionId,
                    browserViewerId,
                    ipAddress);

                var result = await sender.Send(command, ct);
                return result.ToNoContentHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Auctions.RecordAuctionView)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent);
    }
}

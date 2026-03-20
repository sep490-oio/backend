using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.CancelAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class CancelAuctionEndpoint : IEndpoint
{
    public sealed record Request([Required]string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.Cancel, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new CancelAuctionCommand(auctionId, request.Reason);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.Cancel)
            .WithName(ApiEndpoint.Names.Auctions.CancelAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
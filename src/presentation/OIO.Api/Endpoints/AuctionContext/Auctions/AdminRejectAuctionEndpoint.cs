using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.AdminRejectAuction;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

/// <summary>
/// Bug #1 fix: admin-only endpoint that flags a Pending/Approved/Scheduled auction for
/// the seller's attention (e.g., counterfeit suspected, missing info, policy violation).
/// Status is preserved — seller can fix and resubmit. Use Cancel (seller) or
/// Terminate-via-Emergency (admin) for terminal removal.
/// </summary>
public sealed class AdminRejectAuctionEndpoint : IEndpoint
{
    public sealed record Request([Required] string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auctions.AdminReject, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AdminRejectAuctionCommand(auctionId, request.Reason);
                var result = await sender.Send(command, ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Auctions.AdminReject)
            .WithName(ApiEndpoint.Names.Auctions.AdminRejectAuction)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

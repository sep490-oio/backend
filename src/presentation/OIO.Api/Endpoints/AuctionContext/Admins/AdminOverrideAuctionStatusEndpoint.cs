using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.Admin.AdminOverrideAuctionStatus;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class AdminOverrideAuctionStatusEndpoint : IEndpoint
{
    public sealed record Request(string NewStatus, string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AdminOverrideAuctionStatus, async (
                Guid auctionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AdminOverrideAuctionStatusCommand(auctionId, request.NewStatus, request.Reason),
                    ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.AdminOverrideAuctionStatus)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

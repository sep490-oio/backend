using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.AssignItemReviewer;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class AssignItemReviewerEndpoint : IEndpoint
{
    public sealed record Request(Guid AdminId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AssignItemReviewer, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AssignItemReviewerCommand(itemId, request.AdminId);
                var result = await sender.Send(command, ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.AssignItemReviewer)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

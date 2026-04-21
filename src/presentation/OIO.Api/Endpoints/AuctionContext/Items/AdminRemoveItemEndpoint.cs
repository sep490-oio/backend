using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.AdminRemoveItem;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

/// <summary>
/// Bug #11 fix: admin-only endpoint to mark an item as Removed in any state
/// (including InAuction). For when the physical item is destroyed/lost or a
/// counterfeit is confirmed after listing. Admin should usually Terminate the
/// auction first via emergency path before calling this.
/// </summary>
public sealed class AdminRemoveItemEndpoint : IEndpoint
{
    public sealed record Request([Required] string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.AdminRemove, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AdminRemoveItemCommand(itemId, request.Reason);
                var result = await sender.Send(command, ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.AdminRemove)
            .WithName(ApiEndpoint.Names.Items.AdminRemoveItem)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

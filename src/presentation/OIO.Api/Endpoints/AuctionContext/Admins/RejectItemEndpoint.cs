using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.RejectItem;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class RejectItemEndpoint : IEndpoint
{
    public sealed record Request([Required]string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.RejectItem, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RejectItemCommand(itemId, request.Reason);
                var result = await sender.Send(command, ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.RejectItem)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

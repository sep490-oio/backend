using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.ReorderItemMedia;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class ReorderItemMediaEndpoint : IEndpoint
{
    public sealed record Request(List<Guid> OrderedMediaIds);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Items.ReorderMedia, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ReorderItemMediaCommand(
                    itemId, request.OrderedMediaIds);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.ManageMedia)
            .WithName(ApiEndpoint.Names.Items.ReorderItemMedia)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();
    }
}
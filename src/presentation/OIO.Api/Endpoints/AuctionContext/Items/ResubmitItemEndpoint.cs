using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.ResubmitItem;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class ResubmitItemEndpoint : IEndpoint
{
    public sealed record Request(bool VerifyByPlatform = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.Resubmit, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ResubmitItemCommand(itemId, request.VerifyByPlatform);
                var result = await sender.Send(command, ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.Resubmit)
            .WithName(ApiEndpoint.Names.Items.ResubmitItem)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

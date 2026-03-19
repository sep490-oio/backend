using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Commands.SubmitItem;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class SubmitItemEndpoint : IEndpoint
{
    public sealed record Request([Required] bool VerifyByPlatform = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Items.Submit, async (
                Guid itemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SubmitItemCommand(itemId, request.VerifyByPlatform);
                var result = await sender.Send(command, ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Items.Create)
            .WithName(ApiEndpoint.Names.Items.SubmitItem)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

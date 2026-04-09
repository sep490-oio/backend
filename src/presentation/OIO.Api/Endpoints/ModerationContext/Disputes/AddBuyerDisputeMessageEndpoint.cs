using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.AddBuyerDisputeMessage;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class AddBuyerDisputeMessageEndpoint : IEndpoint
{
    public sealed record Request([Required] string Content);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.AddBuyerDisputeMessage, async (
                Guid disputeId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AddBuyerDisputeMessageCommand(disputeId, request.Content), ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.AddBuyerDisputeMessage)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.AddBuyerDisputeEvidence;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class AddBuyerDisputeEvidenceEndpoint : IEndpoint
{
    public sealed record Request([Required] IReadOnlyList<Guid> MediaUploadIds);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.AddBuyerDisputeEvidence, async (
                Guid disputeId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AddBuyerDisputeEvidenceCommand(disputeId, request.MediaUploadIds), ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.AddBuyerDisputeEvidence)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

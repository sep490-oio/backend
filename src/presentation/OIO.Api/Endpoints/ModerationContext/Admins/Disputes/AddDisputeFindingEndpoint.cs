using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.AddDisputeFinding;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins.Disputes;

public sealed class AddDisputeFindingEndpoint : IEndpoint
{
    public sealed record ReferenceRequest(
        [Required] string ReferenceType,
        [Required] Guid TargetId);

    public sealed record Request(
        [Required] string Domain,
        [Required] string Summary,
        string? VerdictRecommendation = null,
        string? FindingNote = null,
        List<ReferenceRequest>? References = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AddDisputeFinding, async (
                Guid disputeId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AddDisputeFindingCommand(
                        disputeId,
                        request.Domain,
                        request.Summary,
                        request.VerdictRecommendation,
                        request.FindingNote,
                        request.References?
                            .Select(r => new AddDisputeFindingReferenceRequest(r.ReferenceType, r.TargetId))
                            .ToList()),
                    ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.AddDisputeFinding)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

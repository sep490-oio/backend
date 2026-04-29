using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetDisputeEligibility;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class GetDisputeEligibilityEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Disputes.Eligibility, async (
                string targetType,
                Guid entityId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetDisputeEligibilityQuery(targetType, entityId), ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Disputes.GetDisputeEligibility)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces<DisputeEligibilityDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

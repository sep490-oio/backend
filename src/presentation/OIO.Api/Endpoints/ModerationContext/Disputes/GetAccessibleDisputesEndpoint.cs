using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetAccessibleDisputes;

namespace OIO.Api.Endpoints.ModerationContext.Disputes;

public sealed class GetAccessibleDisputesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Disputes.GetMine, async (
                string? status,
                Guid? assignedTo,
                int? pageNumber,
                int? pageSize,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetAccessibleDisputesQuery(
                        new DisputeFilterParameters
                        {
                            Status = status,
                            AssignedTo = assignedTo,
                            PageNumber = pageNumber,
                            PageSize = pageSize
                        }),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Disputes.GetAccessibleDisputes)
            .WithTags(ApiEndpoint.Tags.Disputes)
            .Produces<PagedList<DisputeSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

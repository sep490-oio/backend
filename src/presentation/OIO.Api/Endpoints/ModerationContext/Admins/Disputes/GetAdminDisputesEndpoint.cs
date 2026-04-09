using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetAdminDisputes;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins.Disputes;

public sealed class GetAdminDisputesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetAdminDisputes, async (
                string? status,
                string? domain,
                string? caseType,
                Guid? assignedToUserId,
                string? search,
                int? pageNumber,
                int? pageSize,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetAdminDisputesQuery(
                        new AdminDisputeFilterParameters
                        {
                            Status = status,
                            Domain = domain,
                            CaseType = caseType,
                            AssignedToUserId = assignedToUserId,
                            Search = search,
                            PageNumber = pageNumber,
                            PageSize = pageSize
                        }),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.GetAdminDisputes)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<PagedList<AdminDisputeListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

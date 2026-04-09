using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetDisputeAssignableUsers;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins.Disputes;

public sealed class GetDisputeAssignableUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetDisputeAssignableUsers, async (
                Guid disputeId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetDisputeAssignableUsersQuery(disputeId), ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.GetDisputeAssignableUsers)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<List<DisputeAssignableUserDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

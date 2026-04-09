using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetAdminDisputeById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins.Disputes;

public sealed class GetAdminDisputeByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetAdminDisputeById, async (
                Guid disputeId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetAdminDisputeByIdQuery(disputeId), ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.GetAdminDisputeById)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<AdminDisputeDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

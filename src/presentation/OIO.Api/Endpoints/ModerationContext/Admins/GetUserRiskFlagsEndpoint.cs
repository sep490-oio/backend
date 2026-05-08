using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetUserRiskFlags;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class GetUserRiskFlagsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetUserRiskFlags, async (Guid userId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUserRiskFlagsQuery(userId), ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageUsers)
            .WithName(ApiEndpoint.Names.Admins.GetUserRiskFlags)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<IReadOnlyList<UserRiskFlagDto>>(StatusCodes.Status200OK);
    }
}

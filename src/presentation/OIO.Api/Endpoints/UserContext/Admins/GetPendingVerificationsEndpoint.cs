using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetPendingVerifications;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class GetPendingVerificationsEndpoint : IEndpoint
{
    public sealed record Parameters : GetPendingVerificationsQueryFilter;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetPendingVerifications, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetPendingVerificationsQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadVerifications)
            .WithName(ApiEndpoint.Names.Admins.GetPendingVerifications)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}

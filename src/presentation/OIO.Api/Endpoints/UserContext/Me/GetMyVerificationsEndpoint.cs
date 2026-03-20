using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetMyVerifications;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class GetMyVerificationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyVerifications, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyVerificationsQuery();

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadVerification)
            .WithName(ApiEndpoint.Names.Me.GetMyVerifications)
            .WithTags(ApiEndpoint.Tags.Verifications);
    }
}

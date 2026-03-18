using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetVerificationById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class GetVerificationByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetVerificationById, async (
                Guid verificationId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetVerificationByIdQuery(verificationId);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadVerifications)
            .WithName(ApiEndpoint.Names.Admins.GetVerificationById)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}

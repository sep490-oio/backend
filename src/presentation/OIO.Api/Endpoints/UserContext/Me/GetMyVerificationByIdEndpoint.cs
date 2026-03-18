using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetMyVerificationById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class GetMyVerificationByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyVerificationById, async (
                Guid verificationId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyVerificationByIdQuery(verificationId);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadVerification)
            .WithName(ApiEndpoint.Names.Me.GetMyVerificationById)
            .WithTags(ApiEndpoint.Tags.Verifications);
    }
}

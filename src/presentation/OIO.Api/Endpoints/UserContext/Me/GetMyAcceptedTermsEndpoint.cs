using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Queries.GetMyAcceptedTerms;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyAcceptedTermsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyAcceptedTerms, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyAcceptedTermsQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadTerms)
            .WithName(ApiEndpoint.Names.Me.GetMyAcceptedTerms)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<IReadOnlyList<TermsAcceptanceDto>>(StatusCodes.Status200OK);
    }
}

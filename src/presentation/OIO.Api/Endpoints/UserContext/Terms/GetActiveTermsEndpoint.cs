using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Queries.GetActiveTerms;

namespace OIO.Api.Endpoints.UserContext.Terms;

public sealed class GetActiveTermsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Terms.GetActive, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetActiveTermsQuery(), ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Terms.GetActiveTerms)
            .WithTags(ApiEndpoint.Tags.Terms)
            .Produces<IReadOnlyList<TermsDocumentDto>>(StatusCodes.Status200OK);
    }
}

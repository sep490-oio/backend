using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Queries.GetActiveTermsByType;

namespace OIO.Api.Endpoints.UserContext.Terms;

public sealed class GetActiveTermsByTypeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Terms.GetActiveByType, async (
                string type,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetActiveTermsByTypeQuery(type), ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Terms.GetActiveTermsByType)
            .WithTags(ApiEndpoint.Tags.Terms)
            .Produces<TermsDocumentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

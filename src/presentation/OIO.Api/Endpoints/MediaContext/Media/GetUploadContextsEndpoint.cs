using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.MediaContext.Queries.GetUploadContexts;

namespace OIO.Api.Endpoints.MediaContext.Media;

public sealed class GetUploadContextsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Media.Contexts, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetUploadContextsQuery();
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Media.GetUploadContexts)
            .WithTags(ApiEndpoint.Tags.Media)
            .Produces(StatusCodes.Status200OK);
    }
}
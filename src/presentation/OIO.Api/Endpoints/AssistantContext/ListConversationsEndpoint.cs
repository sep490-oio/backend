using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AssistantContext.Queries;
using OIO.Application.Context.AssistantContext.Queries.ListConversations;

namespace OIO.Api.Endpoints.AssistantContext;

public sealed class ListConversationsEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Assistant.ListConversations, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new ListConversationsQuery(parameters);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Assistant.ListConversations)
            .WithTags(ApiEndpoint.Tags.Assistant)
            .Produces<PagedList<AssistantConversationDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

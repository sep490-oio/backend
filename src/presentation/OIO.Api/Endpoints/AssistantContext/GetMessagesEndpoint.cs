using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AssistantContext.Queries;
using OIO.Application.Context.AssistantContext.Queries.GetMessages;

namespace OIO.Api.Endpoints.AssistantContext;

public sealed class GetMessagesEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Assistant.GetMessages, async (
                [FromRoute] Guid conversationId,
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMessagesQuery(conversationId, parameters);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Assistant.GetMessages)
            .WithTags(ApiEndpoint.Tags.Assistant)
            .Produces<PagedList<AssistantMessageDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}

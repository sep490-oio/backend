using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.AssistantContext.Commands.CreateConversation;
using OIO.Application.Context.UserContext.Services;

namespace OIO.Api.Endpoints.AssistantContext;

public sealed class CreateConversationEndpoint : IEndpoint
{
    public sealed record Body(string? RoleContext, string? Title);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Assistant.CreateConversation, async (
                [FromBody] Body body,
                ISender sender,
                ICurrentUser currentUser,
                CancellationToken ct) =>
            {
                var userId = currentUser.IsAuthenticated ? currentUser.UserId.Value : (Guid?)null;
                var role = string.IsNullOrWhiteSpace(body?.RoleContext) ? "guest" : body!.RoleContext!;
                var title = string.IsNullOrWhiteSpace(body?.Title) ? "Hỗ trợ AI" : body!.Title!;

                var command = new CreateConversationCommand(userId, role, title);
                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Assistant.CreateConversation)
            .WithTags(ApiEndpoint.Tags.Assistant)
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}

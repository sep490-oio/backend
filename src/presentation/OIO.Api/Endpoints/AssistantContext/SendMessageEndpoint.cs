using System.Collections.Concurrent;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.AssistantContext.Commands.SendMessage;
using OIO.Application.Context.AssistantContext.Services.Models;

namespace OIO.Api.Endpoints.AssistantContext;

public sealed class SendMessageEndpoint : IEndpoint
{
    public sealed record Body(
        string UserText,
        string? Locale,
        PageContextDto? Page);

    public sealed record PageContextDto(
        string? Route,
        string? EntityType,
        Guid? EntityId);

    // Cross-conversation IP rate limit (denial-of-wallet protection).
    // The per-conversation rate limit in SendMessageCommandHandler can be bypassed
    // by spamming CreateConversation, so we also bucket by remote IP at the edge.
    private const int MaxAnonymousMessagesPerIpPerMinute = 30;
    private static readonly ConcurrentDictionary<string, RateBucket> IpBuckets = new();

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Assistant.SendMessage, async (
                [FromRoute] Guid conversationId,
                [FromBody] Body body,
                HttpContext httpContext,
                ISender sender,
                CancellationToken ct) =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var bucket = IpBuckets.GetOrAdd(ip, _ => new RateBucket());
                if (!bucket.TryConsume(MaxAnonymousMessagesPerIpPerMinute))
                {
                    return Results.Problem(
                        title: "Rate limit exceeded",
                        detail: "Too many requests. Please wait and try again.",
                        statusCode: StatusCodes.Status429TooManyRequests);
                }

                var page = new PageContext(
                    Route: body.Page?.Route ?? "/",
                    EntityType: body.Page?.EntityType,
                    EntityId: body.Page?.EntityId);

                var command = new SendMessageCommand(
                    ConversationId: conversationId,
                    UserText: body.UserText,
                    Locale: body.Locale ?? "vi",
                    Page: page);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Assistant.SendMessage)
            .WithTags(ApiEndpoint.Tags.Assistant)
            .Produces<SendMessageResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private sealed class RateBucket
    {
        private readonly Lock _lock = new();
        private readonly Queue<DateTime> _hits = new();

        public bool TryConsume(int maxPerMinute)
        {
            lock (_lock)
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-1);
                while (_hits.Count > 0 && _hits.Peek() < cutoff)
                    _hits.Dequeue();

                if (_hits.Count >= maxPerMinute)
                    return false;

                _hits.Enqueue(DateTime.UtcNow);
                return true;
            }
        }
    }
}

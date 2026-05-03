using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AssistantContext.Services;
using OIO.Application.Context.AssistantContext.Services.Models;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AssistantContext.Aggregates;
using OIO.Domain.Context.AssistantContext.Enums;
using OIO.Domain.Context.AssistantContext.Errors;
using OIO.Domain.Context.AssistantContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AssistantContext.Commands.SendMessage;

internal sealed class SendMessageCommandHandler
    : ICommandHandler<SendMessageCommand, SendMessageResult>
{
    private const int MaxUserTextLength = 2000;

    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAssistantChatService _chatService;
    private readonly ICurrentUser _currentUser;
    private readonly IOptions<AssistantOptions> _options;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public SendMessageCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IAssistantChatService chatService,
        ICurrentUser currentUser,
        IOptions<AssistantOptions> options,
        ILogger<SendMessageCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _chatService = chatService;
        _currentUser = currentUser;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<SendMessageResult, Error>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserText) || request.UserText.Length > MaxUserTextLength)
            return AssistantErrors.InvalidInput;

        var conversationId = AssistantConversationId.From(request.ConversationId);

        var conversation = await _dbContext.Set<AssistantConversation>()
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null)
            return AssistantErrors.ConversationNotFound;

        // Authz: authenticated user must own the conversation, or guest conversation
        // (UserId null) is reachable by any caller for v1.
        if (conversation.UserId is not null)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId != conversation.UserId.Value)
                return AssistantErrors.ConversationAccessDenied;
        }

        // Per-conversation rate limit (denial-of-wallet protection on Gemini API).
        var rateLimitPerMinute = Math.Max(1, _options.Value.RateLimitPerMinutePerUser);
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
        var recentUserMessageCount = await _dbContext.Set<AssistantMessage>()
            .CountAsync(
                m => m.ConversationId == conversationId
                    && m.Sender == AssistantSender.User
                    && m.CreatedAt > oneMinuteAgo,
                cancellationToken);
        if (recentUserMessageCount >= rateLimitPerMinute)
        {
            _logger.LogInformation(
                "Assistant rate limit hit: ConversationId={ConversationId} count={Count}",
                conversationId.Value, recentUserMessageCount);
            return AssistantErrors.RateLimited;
        }

        // Load recent history for prompt context.
        var historyEntities = await _dbContext.Set<AssistantMessage>()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(12)
            .ToListAsync(cancellationToken);

        historyEntities.Reverse();

        var history = historyEntities
            .Select(m => new ConversationHistoryMessage(m.Sender, m.Content))
            .ToList();

        var role = conversation.RoleContext;
        var context = new AssistantRequestContext(
            UserId: conversation.UserId?.Value,
            Role: role,
            Locale: string.IsNullOrWhiteSpace(request.Locale) ? "vi" : request.Locale,
            Page: request.Page);

        // Persist user message first so we always have it logged even on provider failure.
        var userMessage = AssistantMessage.Create(
            conversation.Id,
            AssistantSender.User,
            request.UserText.Trim());
        _dbContext.Insert(userMessage);
        conversation.TouchLastMessage(userMessage.CreatedAt);

        var chatResult = await _chatService.SendAsync(
            new AssistantChatRequest(context, request.UserText.Trim(), history),
            cancellationToken);

        if (chatResult.IsFailure)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return chatResult.Error;
        }

        var response = chatResult.Value;
        var citationsJson = response.Citations.Count > 0
            ? JsonSerializer.Serialize(response.Citations, JsonOpts)
            : null;
        var metadata = new
        {
            confidence = response.Confidence,
            needsHumanSupport = response.NeedsHumanSupport,
            suggestedActions = response.SuggestedActions
        };
        var metadataJson = JsonSerializer.Serialize(metadata, JsonOpts);

        var assistantMessage = AssistantMessage.Create(
            conversation.Id,
            AssistantSender.Assistant,
            response.Answer,
            citationsJson,
            metadataJson,
            response.TokenUsage);
        _dbContext.Insert(assistantMessage);
        conversation.TouchLastMessage(assistantMessage.CreatedAt);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Assistant reply: ConversationId={ConversationId} Route={Route} Confidence={Confidence} NeedsHuman={Needs}",
            conversation.Id.Value, request.Page.Route, response.Confidence, response.NeedsHumanSupport);

        return new SendMessageResult(
            assistantMessage.Id.Value,
            response.Answer,
            response.Citations,
            response.SuggestedActions,
            response.Confidence,
            response.NeedsHumanSupport);
    }
}

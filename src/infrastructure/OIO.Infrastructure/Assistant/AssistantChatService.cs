using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AssistantContext.Services;
using OIO.Application.Context.AssistantContext.Services.Models;
using OIO.Domain.Context.AssistantContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Assistant;

internal sealed class AssistantChatService : IAssistantChatService
{
    private readonly IAssistantChatClient _chatClient;
    private readonly IAssistantKnowledgeRetriever _retriever;
    private readonly IAssistantToolExecutor _toolExecutor;
    private readonly IAssistantSafetyService _safety;
    private readonly IOptions<AssistantOptions> _options;
    private readonly ILogger<AssistantChatService> _logger;

    private static readonly JsonSerializerOptions JsonReadOpts = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public AssistantChatService(
        IAssistantChatClient chatClient,
        IAssistantKnowledgeRetriever retriever,
        IAssistantToolExecutor toolExecutor,
        IAssistantSafetyService safety,
        IOptions<AssistantOptions> options,
        ILogger<AssistantChatService> logger)
    {
        _chatClient = chatClient;
        _retriever = retriever;
        _toolExecutor = toolExecutor;
        _safety = safety;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<AssistantChatResponse, Error>> SendAsync(
        AssistantChatRequest request,
        CancellationToken ct)
    {
        if (!_chatClient.IsEnabled)
        {
            return new AssistantChatResponse(
                Answer: "Trợ lý đang bảo trì. Vui lòng thử lại sau.",
                Citations: [],
                SuggestedActions: [],
                Confidence: 0,
                NeedsHumanSupport: true);
        }

        if (_safety.IsPromptInjection(request.UserText))
        {
            _logger.LogWarning("Assistant: prompt-injection attempt detected, route={Route}",
                request.Context.Page.Route);
            return new AssistantChatResponse(
                Answer: "Tin nhắn của bạn chứa nội dung tôi không thể xử lý. Vui lòng diễn đạt lại.",
                Citations: [],
                SuggestedActions: [],
                Confidence: 0,
                NeedsHumanSupport: false);
        }

        var opts = _options.Value;
        var knowledge = _retriever.Retrieve(request.UserText, opts.KnowledgeTopN);

        // v1: do not call tool executor — keep the response purely RAG-based.
        // (Executor is wired so v2 can add safe tools without changing this code.)
        IReadOnlyList<ToolResult> toolResults = [];

        var messages = AssistantSystemPromptBuilder.Build(
            request,
            knowledge,
            toolResults,
            opts.RecentHistoryCount);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(opts.TimeoutSeconds));

        ChatResponse response;
        try
        {
            response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    Temperature = (float)opts.Temperature,
                    MaxOutputTokens = opts.MaxResponseTokens,
                    ResponseFormat = ChatResponseFormat.Json,
                },
                cts.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Assistant provider timed out: Model={Model}", opts.Model);
            return AssistantErrors.ProviderUnavailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Assistant provider failed: Model={Model} ExceptionType={ExceptionType}",
                opts.Model, ex.GetType().Name);
            return AssistantErrors.ProviderUnavailable;
        }

        var text = ExtractText(response);
        var parsed = TryParse(text);

        if (parsed is null)
        {
            _logger.LogWarning("Assistant returned malformed JSON; falling back.");
            return new AssistantChatResponse(
                Answer: "Tôi chưa có đủ thông tin để trả lời chính xác. Bạn có thể diễn đạt rõ hơn hoặc liên hệ hỗ trợ.",
                Citations: BuildCitations(knowledge),
                SuggestedActions: [],
                Confidence: 0.2,
                NeedsHumanSupport: true);
        }

        var suggestedActions = BuildSuggestedActions(parsed.SuggestedActionHints, request.Context);

        return new AssistantChatResponse(
            Answer: parsed.Answer,
            Citations: BuildCitations(knowledge),
            SuggestedActions: suggestedActions,
            Confidence: Math.Clamp(parsed.Confidence, 0, 1),
            NeedsHumanSupport: parsed.NeedsHumanSupport);
    }

    private static IReadOnlyList<Citation> BuildCitations(IReadOnlyList<KnowledgeEntry> knowledge)
        => knowledge.Select(k => new Citation(k.Title, k.SourceUrl)).ToList();

    private static IReadOnlyList<SuggestedAction> BuildSuggestedActions(
        IReadOnlyList<SuggestedActionHint>? hints,
        AssistantRequestContext context)
    {
        if (hints is null || hints.Count == 0)
            return [];

        var resolved = new List<SuggestedAction>(hints.Count);
        foreach (var hint in hints)
        {
            if (string.IsNullOrWhiteSpace(hint.Label) || string.IsNullOrWhiteSpace(hint.Route))
                continue;
            var route = hint.Route.Replace("{id}",
                context.Page.EntityId?.ToString("D") ?? "");
            // Disallow external links — only same-origin paths.
            // Block protocol-relative URLs ("//evil.com") which are absolute despite starting with '/'.
            if (!route.StartsWith('/') || route.StartsWith("//"))
                continue;
            resolved.Add(new SuggestedAction(hint.Label.Trim(), route));
            if (resolved.Count >= 4)
                break;
        }
        return resolved;
    }

    private static string ExtractText(ChatResponse response)
    {
        var text = response.Text;
        if (!string.IsNullOrEmpty(text))
            return text;

        if (response.Messages is { Count: > 0 } messages)
            return messages[^1].Text ?? string.Empty;

        return string.Empty;
    }

    private static StructuredAssistantPayload? TryParse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            // Try strict parse first.
            var payload = JsonSerializer.Deserialize<StructuredAssistantPayload>(text, JsonReadOpts);
            if (payload is { Answer: not null })
                return payload;
        }
        catch (JsonException) { /* try recover */ }

        // Recover: extract first {…} block.
        var firstBrace = text.IndexOf('{');
        var lastBrace = text.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace)
            return null;

        try
        {
            var slice = text[firstBrace..(lastBrace + 1)];
            var payload = JsonSerializer.Deserialize<StructuredAssistantPayload>(slice, JsonReadOpts);
            if (payload is { Answer: not null })
                return payload;
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private sealed record StructuredAssistantPayload(
        string Answer,
        double Confidence,
        bool NeedsHumanSupport,
        IReadOnlyList<SuggestedActionHint>? SuggestedActionHints);

    private sealed record SuggestedActionHint(string Label, string Route);
}

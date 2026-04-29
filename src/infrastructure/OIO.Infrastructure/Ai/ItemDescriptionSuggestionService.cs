using CSharpFunctionalExtensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AuctionContext.Services.AiSuggestion;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Ai;

internal sealed class ItemDescriptionSuggestionService : IItemDescriptionSuggestionService
{
    private readonly IChatClient _chatClient;
    private readonly IOptions<AiOptions> _options;
    private readonly ILogger<ItemDescriptionSuggestionService> _logger;

    public ItemDescriptionSuggestionService(
        IChatClient chatClient,
        IOptions<AiOptions> options,
        ILogger<ItemDescriptionSuggestionService> logger)
    {
        _chatClient = chatClient;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<RawSuggestion, Error>> SuggestAsync(
        SuggestItemDescriptionInput input,
        CancellationToken ct)
    {
        var opts = _options.Value;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(opts.TimeoutSeconds));

        var messages = SuggestionPromptBuilder.Build(input, opts);

        ChatResponse response;
        try
        {
            response = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    Temperature = (float)opts.Temperature,
                    ResponseFormat = ChatResponseFormat.Json,
                },
                cts.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Caller cancelled — propagate.
            throw;
        }
        catch (OperationCanceledException)
        {
            // Provider timeout via our linked CTS.
            _logger.LogWarning(
                "AI provider timed out: Model={Model} TimeoutSeconds={TimeoutSeconds}",
                opts.Model, opts.TimeoutSeconds);
            return AiSuggestionErrors.ProviderUnavailable;
        }
        catch (Exception ex)
        {
            // Sanitize: log only metadata, never the inner message body which may
            // contain prompt fragments or URLs returned by the provider.
            _logger.LogWarning(
                "AI provider failed: Model={Model} ExceptionType={ExceptionType}",
                opts.Model, ex.GetType().Name);
            return AiSuggestionErrors.ProviderUnavailable;
        }

        var text = ExtractText(response);
        var (raw, error) = SuggestionResponseParser.Parse(text, opts);

        if (raw is null)
        {
            _logger.LogWarning(
                "AI returned malformed payload: Model={Model} Reason={Reason}",
                opts.Model, error);
            return AiSuggestionErrors.ProviderUnavailable;
        }

        return raw;
    }

    /// <summary>
    /// Extract the joined text from a ChatResponse. The Microsoft.Extensions.AI
    /// API surface evolves between previews; we probe the most stable accessors.
    /// </summary>
    private static string ExtractText(ChatResponse response)
    {
        // Preferred accessor in current preview.
        var text = response.Text;
        if (!string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Fallback: walk the messages and concatenate text contents.
        if (response.Messages is { Count: > 0 } messages)
        {
            var last = messages[^1];
            return last.Text ?? string.Empty;
        }

        return string.Empty;
    }
}

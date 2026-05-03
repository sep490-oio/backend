using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;

namespace OIO.Infrastructure.Assistant;

internal sealed class AssistantChatClient : IAssistantChatClient, IDisposable
{
    private readonly IChatClient? _chatClient;
    private readonly bool _enabled;

    public AssistantChatClient(IOptions<AssistantOptions> options, ILoggerFactory loggerFactory)
    {
        var opts = options.Value;
        _enabled = opts.Enabled
            && !string.IsNullOrWhiteSpace(opts.ApiKey)
            && !string.IsNullOrWhiteSpace(opts.Model);

        if (!_enabled)
            return;

        var client = new global::Google.GenAI.Client(
            vertexAI: false,
            apiKey: opts.ApiKey,
            credential: null,
            project: null,
            location: null,
            httpOptions: null);

        _chatClient = client
            .AsIChatClient(opts.Model)
            .AsBuilder()
            .UseOpenTelemetry(loggerFactory)
            .Build();
    }

    public bool IsEnabled => _enabled && _chatClient is not null;

    public Task<ChatResponse> GetResponseAsync(
        IList<ChatMessage> messages,
        ChatOptions options,
        CancellationToken cancellationToken)
    {
        if (_chatClient is null)
            throw new InvalidOperationException("Assistant chat client is not enabled.");
        return _chatClient.GetResponseAsync(messages, options, cancellationToken);
    }

    public void Dispose() => _chatClient?.Dispose();
}

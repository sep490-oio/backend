using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AssistantContext.Services.Models;

namespace OIO.Application.Context.AssistantContext.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ConversationId,
    string UserText,
    string Locale,
    PageContext Page) : ICommand<SendMessageResult>;

public sealed record SendMessageResult(
    Guid MessageId,
    string Answer,
    IReadOnlyList<Citation> Citations,
    IReadOnlyList<SuggestedAction> SuggestedActions,
    double Confidence,
    bool NeedsHumanSupport);

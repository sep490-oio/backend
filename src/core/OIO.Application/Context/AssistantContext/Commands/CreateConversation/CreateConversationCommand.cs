using OIO.Application.Abstractions.Messaging;

namespace OIO.Application.Context.AssistantContext.Commands.CreateConversation;

public sealed record CreateConversationCommand(
    Guid? UserId,
    string RoleContext,
    string? Title) : ICommand<Guid>;

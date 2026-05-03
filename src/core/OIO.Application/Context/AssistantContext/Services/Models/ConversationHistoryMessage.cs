using OIO.Domain.Context.AssistantContext.Enums;

namespace OIO.Application.Context.AssistantContext.Services.Models;

public sealed record ConversationHistoryMessage(AssistantSender Sender, string Content);

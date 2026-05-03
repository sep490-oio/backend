using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;

namespace OIO.Application.Context.AssistantContext.Queries.GetMessages;

public sealed record GetMessagesQuery(Guid ConversationId, PagedParameters Paging)
    : IQuery<PagedList<AssistantMessageDto>>;

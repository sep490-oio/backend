using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;

namespace OIO.Application.Context.AssistantContext.Queries.ListConversations;

public sealed record ListConversationsQuery(PagedParameters Paging)
    : IQuery<PagedList<AssistantConversationDto>>;

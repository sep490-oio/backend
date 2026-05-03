using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AssistantContext.Aggregates;
using OIO.Domain.Context.AssistantContext.Errors;
using OIO.Domain.Context.AssistantContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AssistantContext.Queries.GetMessages;

internal sealed class GetMessagesQueryHandler
    : IQueryHandler<GetMessagesQuery, PagedList<AssistantMessageDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMessagesQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<AssistantMessageDto>, Error>> Handle(
        GetMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var conversationId = AssistantConversationId.From(request.ConversationId);

        var conversation = await _dbContext.Set<AssistantConversation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null)
            return AssistantErrors.ConversationNotFound;

        if (conversation.UserId is not null)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId != conversation.UserId.Value)
                return AssistantErrors.ConversationAccessDenied;
        }

        var page = request.Paging.EffectivePageNumber;
        var size = request.Paging.EffectivePageSize;

        var baseQuery = _dbContext.Set<AssistantMessage>()
            .Where(m => m.ConversationId == conversationId);

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(m => new AssistantMessageDto(
                m.Id.Value,
                m.ConversationId.Value,
                m.Sender.ToString().ToLowerInvariant(),
                m.Content,
                m.Citations,
                m.Metadata,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedList<AssistantMessageDto>(items, total, page, size);
    }
}

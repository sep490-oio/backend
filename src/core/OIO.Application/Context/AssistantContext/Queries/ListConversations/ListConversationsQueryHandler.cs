using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AssistantContext.Aggregates;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AssistantContext.Queries.ListConversations;

internal sealed class ListConversationsQueryHandler
    : IQueryHandler<ListConversationsQuery, PagedList<AssistantConversationDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListConversationsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<AssistantConversationDto>, Error>> Handle(
        ListConversationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return PagedList<AssistantConversationDto>.Empty();

        var userId = _currentUser.UserId;

        var query = _dbContext.Set<AssistantConversation>()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt);

        var total = await query.CountAsync(cancellationToken);

        var page = request.Paging.EffectivePageNumber;
        var size = request.Paging.EffectivePageSize;

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new AssistantConversationDto(
                c.Id.Value,
                c.RoleContext,
                c.Title,
                c.CreatedAt,
                c.LastMessageAt))
            .ToListAsync(cancellationToken);

        return new PagedList<AssistantConversationDto>(items, total, page, size);
    }
}

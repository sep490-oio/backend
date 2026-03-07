using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyItems;

public sealed record GetMyItemsQuery : IQuery<IReadOnlyList<ItemDto>>;

internal sealed class GetMyItemsQueryHandler
    : IQueryHandler<GetMyItemsQuery, IReadOnlyList<ItemDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyItemsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ItemDto>, Error>> Handle(
        GetMyItemsQuery request,
        CancellationToken cancellationToken)
    {

        var items = await _dbContext.Set<Item>()
            .Include(i => i.Media.OrderBy(img => img.SortOrder))
            .Where(i => i.SellerId == _currentUser.UserId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);

        return items;
    }
}
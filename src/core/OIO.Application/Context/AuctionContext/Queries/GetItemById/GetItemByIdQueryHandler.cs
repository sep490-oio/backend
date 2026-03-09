using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetItemById;

internal sealed class GetItemByIdQueryHandler
    : IQueryHandler<GetItemByIdQuery, ItemDto>
{
    private readonly IDbContext _dbContext;

    public GetItemByIdQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ItemDto, Error>> Handle(
        GetItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        
        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(x => x.Media),
            cancellationToken:cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        return item.ToDto();
    }
}
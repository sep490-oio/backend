using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyItems;

internal sealed class GetMyItemsQueryHandler
    : IQueryHandler<GetMyItemsQuery, PagedList<ItemDto>>
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

    public async Task<Result<PagedList<ItemDto>, Error>> Handle(
        GetMyItemsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<Item>()
            .AsNoTracking()
            .Where(i => i.SellerId == _currentUser.UserId)
            .ApplySort(parameters, ItemMappings.ItemDtoSortMapping);

        var totalCount = await query.CountAsync(cancellationToken);
        

        var items = await query
            .Select(item => new ItemDto(
                Id: item.Id.Value,
                SellerId: item.SellerId.Value,
                CategoryId: item.CategoryId.HasValue ? item.CategoryId.Value.Value : null,
                Title: item.Title.Value,
                Description: item.Description,
                Condition: item.Condition.Id,
                Status: item.Status.Id,
                Quantity: item.Quantity,
                Images: item.Media
                    .Select(media => 
                        new ItemMediaDto(
                            Id: media.Id.Value,
                            Url: media.Info.SecureUrl,
                            PublicId: media.StorageRef.PublicId,
                            ResourceType: media.ResourceType,
                            IsPrimary: media.IsPrimary,
                            SortOrder: media.SortOrder,
                            FileName: media.Info.FileName,
                            Bytes: media.Info.Bytes,
                            Format: media.Info.Format,
                            Width: media.Info.Width,
                            Height: media.Info.Height,
                            DurationSeconds: media.Info.DurationSeconds))
                    .ToList(),
                CreatedAt: item.CreatedAt))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return items;
    }
}
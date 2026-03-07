using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.ReorderItemMedia;

public sealed record ReorderItemMediaCommand(
    Guid ItemId,
    IReadOnlyList<Guid> OrderedMediaIds) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ReorderItemMediaCommand.Check()
            .WithOwnerName("ReorderItemMedia")
            .Field(ItemId)
            .NotEmptyGuid()
            .ToViolationsError();
    }
}

internal sealed class ReorderItemMediaCommandHandler
    : ICommandHandler<ReorderItemMediaCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ReorderItemMediaCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ReorderItemMediaCommand request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        
        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: query => query
                .Include(x => x.Media),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        if (item.SellerId != _currentUser.UserId)
            return AuctionErrors.Item.NotOwnedByUser(itemId, _currentUser.UserId);

        var nowUtc = _clock.UtcNow; 
        
        // Validate all IDs belong to this item
        var itemMediaIds = item.Media.Select(m => m.Id).ToHashSet();
        var invalidIds = request.OrderedMediaIds
            .Where(id => !itemMediaIds.Contains(ItemMediaId.From(id)))
            .ToList();

        if (invalidIds.Count > 0)
            return MediaErrors.NotFounds(string.Join(", ", invalidIds));

        item.ReorderMedia(nowUtc, request.OrderedMediaIds.Select(ItemMediaId.From).ToList());
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success<Error>();
    }
}


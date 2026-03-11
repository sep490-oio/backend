using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.Commands.ActivateItem;

public sealed record ActivateItemCommand(Guid ItemId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ActivateItemCommand.Check()
            .WithOwnerName("ActivateItem")
            .Field(ItemId)
            .NotEmptyGuid();
    }
}

internal sealed class ActivateItemCommandHandler
    : ICommandHandler<ActivateItemCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ActivateItemCommandHandler(
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
        ActivateItemCommand request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        
        var item = await _dbContext.GetByIdAsync<Item ,ItemId>(
            id: itemId,
            queryBuilder: query => query
                .Include(x => x.Media),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        if (item.SellerId != _currentUser.UserId)
            return AuctionErrors.Item.NotOwnedByUser(itemId, _currentUser.UserId);

        var nowUtc = _clock.UtcNow;
        
        var result = item.Activate(nowUtc);

        if (result.IsFailure)
            return result.Error;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
       
    }
}

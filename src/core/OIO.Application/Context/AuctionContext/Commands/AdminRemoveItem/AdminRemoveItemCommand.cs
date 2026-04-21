using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.AdminRemoveItem;

/// <summary>
/// Bug #11 fix: admin endpoint to mark an item as Removed in any state
/// (including InAuction). Use cases: physical item destroyed/lost, counterfeit
/// confirmed, admin cleanup. Does NOT automatically cancel any active auction —
/// admin should first Terminate the auction (via emergency), then Remove the item.
/// </summary>
public sealed record AdminRemoveItemCommand(
    Guid ItemId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        AdminRemoveItemCommand.Check()
            .WithOwnerName("AdminRemoveItem")
            .Field(ItemId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class AdminRemoveItemCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<AdminRemoveItemCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminRemoveItemCommand request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);

        var item = await dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: q => q.AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        var nowUtc = clock.UtcNow;

        var result = item.Remove(nowUtc);
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}

using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.RejectItem;

public sealed record RejectItemCommand(
    Guid ItemId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RejectItemCommand.Check()
            .WithOwnerName("RejectItem")
            .Field(ItemId)
            .NotEmptyGuid()
            .Field(Reason)
            .NotWhiteSpace()
            .MaxLength(1000);
    }
}

internal sealed class RejectItemCommandHandler : ICommandHandler<RejectItemCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public RejectItemCommandHandler(
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
        RejectItemCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var itemId = ItemId.From(request.ItemId);
        var adminId = _currentUser.UserId;

        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: q => q.Include(i => i.ModerationReviews),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        if (item.Status != ItemStatus.PendingReview)
            return AuctionErrors.Item.InvalidState(item.Status.Id, "reject");

        var result = item.Reject(adminId, request.Reason, nowUtc);
        if (result.IsFailure) return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}

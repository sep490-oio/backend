using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.AssignItemReviewer;

public sealed record AssignItemReviewerCommand(
    Guid ItemId,
    Guid AdminId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AssignItemReviewerCommand.Check()
            .WithOwnerName("AssignItemReviewer")
            .Field(ItemId)
            .NotEmptyGuid()
            .Field(AdminId)
            .NotEmptyGuid();
    }
}

internal sealed class AssignItemReviewerCommandHandler : ICommandHandler<AssignItemReviewerCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AssignItemReviewerCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        AssignItemReviewerCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var itemId = ItemId.From(request.ItemId);
        var adminId = UserId.From(request.AdminId);

        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: q => q.Include(i => i.ModerationReviews),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        if (item.Status != ItemStatus.Draft &&
            item.Status != ItemStatus.PendingReview)
            return AuctionErrors.Item.InvalidState(item.Status.Id, "assign reviewer");

        var result = item.AssignAdmin(adminId, nowUtc);
        if (result.IsFailure) return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}

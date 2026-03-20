using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.SubmitItem;

public sealed record SubmitItemCommand(
    Guid ItemId,
    bool VerifyByPlatform = false) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return SubmitItemCommand.Check()
            .WithOwnerName("SubmitItem")
            .Field(ItemId)
            .NotEmptyGuid();
    }
}

internal sealed class SubmitItemCommandHandler : ICommandHandler<SubmitItemCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ISender _sender;
    private readonly ILogger<SubmitItemCommandHandler> _logger;

    public SubmitItemCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        ISender sender,
        ILogger<SubmitItemCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _sender = sender;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(
        SubmitItemCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var itemId = ItemId.From(request.ItemId);

        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: q => q
                .Include(i => i.Media)
                .Include(i => i.ModerationReviews)
                .Include(i => i.Auctions),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        if (item.SellerId != _currentUser.UserId)
            return AuctionErrors.Item.NotOwnedByUser(itemId, _currentUser.UserId);

        if (item.Status != ItemStatus.Draft)
            return AuctionErrors.Item.InvalidState(item.Status.Id, "submit");

        var result = item.Submit(request.VerifyByPlatform, nowUtc);
        if (result.IsFailure)
            return result.Error;

        var auctionId = item.Auctions
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => (Guid?)a.Id.Value)
            .FirstOrDefault();

        var assignedAdminId = item.AssignedAdminId;
        if (!request.VerifyByPlatform && assignedAdminId is null)
        {
            assignedAdminId = await AuctionReviewAssignments.ResolveReviewerIdAsync(
                _dbContext,
                cancellationToken);

            if (assignedAdminId is not null)
            {
                var assignResult = item.AssignAdmin(assignedAdminId.Value, nowUtc);
                if (assignResult.IsFailure)
                    return assignResult.Error;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.VerifyByPlatform)
        {
            await NotificationDispatch.DispatchAsync(
                _sender,
                _logger,
                new CreateNotificationCommand(
                    UserId: item.SellerId.Value,
                    NotificationType: "moderation",
                    EventType: "item_shipping_required",
                    Title: "Can khai bao shipping cho buoc kiem dinh",
                    Message:
                        $"San pham \"{item.Title.Value}\" da duoc gui sang buoc kiem dinh. " +
                        $"Vui long goi POST /api/items/{item.Id.Value}/shipping de tiep tuc.",
                    Priority: NotificationPriority.High,
                    EntityType: "Item",
                    EntityId: item.Id.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId,
                        itemId = item.Id.Value,
                        verifyByPlatform = true
                    }),
                    Actions: NotificationDispatch.SerializeMetadata(new[]
                    {
                        new
                        {
                            type = "choose_item_shipping",
                            label = "Choose shipping",
                            method = "POST",
                            endpoint = $"api/items/{item.Id.Value}/shipping"
                        }
                    })),
                cancellationToken);
        }
        else if (auctionId.HasValue)
        {
            await NotificationDispatch.DispatchAsync(
                _sender,
                _logger,
                new CreateNotificationCommand(
                    UserId: item.SellerId.Value,
                    NotificationType: "moderation",
                    EventType: "item_submitted_for_review",
                    Title: "San pham da vao hang doi duyet",
                    Message: $"San pham \"{item.Title.Value}\" da duoc dua vao hang doi duyet cua admin.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId = auctionId.Value,
                        itemId = item.Id.Value,
                        verifyByPlatform = false,
                        assignedAdminId = assignedAdminId?.Value
                    })),
                cancellationToken);
        }

        return UnitResult.Success<Error>();
    }
}

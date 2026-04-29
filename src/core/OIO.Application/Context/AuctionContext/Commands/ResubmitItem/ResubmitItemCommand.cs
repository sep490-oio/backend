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
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.ResubmitItem;

public sealed record ResubmitItemCommand(
    Guid ItemId,
    bool VerifyByPlatform = false) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ResubmitItemCommand.Check()
            .WithOwnerName("ResubmitItem")
            .Field(ItemId)
            .NotEmptyGuid();
    }
}

internal sealed class ResubmitItemCommandHandler : ICommandHandler<ResubmitItemCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ISender _sender;
    private readonly ILogger<ResubmitItemCommandHandler> _logger;

    public ResubmitItemCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        ISender sender,
        ILogger<ResubmitItemCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _sender = sender;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(
        ResubmitItemCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var itemId = ItemId.From(request.ItemId);

        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: q => q
                .Include(i => i.ModerationReviews)
                .Include(i => i.Auctions),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        if (item.SellerId != _currentUser.UserId)
            return AuctionErrors.Item.NotOwnedByUser(itemId, _currentUser.UserId);

        if (item.Status != ItemStatus.Rejected)
            return AuctionErrors.Item.InvalidState(item.Status.Id, "resubmit");

        // Warehouse-bound items must NOT be funneled back into online review.
        // Any of three signals classifies the item as warehouse-bound:
        //   1. The canonical RequiresPlatformInspection flag (set on Submit/Resubmit).
        //   2. A WarehouseInspection record (item physically reached warehouse).
        //   3. A still-active (non-dispatched, non-closed) WarehouseItem row.
        // Sellers in this branch must call POST /api/seller/warehouse/items/{id}/request-reinspection
        // (item still at warehouse) or open a new inbound shipment after the return shipment closes.
        var hasWarehouseInspection = await _dbContext.Set<WarehouseInspection>()
            .AsNoTracking()
            .AnyAsync(wi => wi.ItemId == request.ItemId, cancellationToken);

        var hasActiveWarehouseItem = await _dbContext.Set<WarehouseItem>()
            .AsNoTracking()
            .AnyAsync(
                wh => wh.ItemId == request.ItemId
                      && wh.Status != WarehouseItemStatus.Dispatched
                      && wh.Status != WarehouseItemStatus.AwaitingDisposition,
                cancellationToken);

        var isWarehouseBound = item.RequiresPlatformInspection
                               || hasWarehouseInspection
                               || hasActiveWarehouseItem;

        if (isWarehouseBound)
        {
            return Error.Conflict(
                "Item.WarehouseRejectedCannotOnlineResubmit",
                "Item này đã thuộc flow kiểm định tại kho, không thể gửi sang review online. " +
                "Vui lòng dùng request-reinspection nếu hàng còn ở kho hoặc tạo inbound mới sau khi nhận hàng trả về.");
        }

        var result = item.Resubmit(request.VerifyByPlatform, nowUtc);
        if (result.IsFailure) return result.Error;

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
                        $"San pham \"{item.Title.Value}\" da duoc gui lai sang buoc kiem dinh. " +
                        $"Vui long goi POST /api/items/{item.Id.Value}/shipping de tiep tuc.",
                    Priority: NotificationPriority.High,
                    EntityType: "Item",
                    EntityId: item.Id.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId,
                        itemId = item.Id.Value,
                        verifyByPlatform = true,
                        isResubmission = true
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
                    EventType: "item_resubmitted_for_review",
                    Title: "San pham da vao lai hang doi duyet",
                    Message: $"San pham \"{item.Title.Value}\" da duoc dua tro lai hang doi duyet cua admin.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId = auctionId.Value,
                        itemId = item.Id.Value,
                        verifyByPlatform = false,
                        assignedAdminId = assignedAdminId?.Value,
                        isResubmission = true
                    })),
                cancellationToken);
        }

        return UnitResult.Success<Error>();
    }
}

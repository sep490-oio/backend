using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.CreateReport;

public sealed record CreateReportCommand(
    string EntityType,
    Guid EntityId,
    string ReasonCode,
    string? Description,
    string? Attachments) : ICommand<ReportDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CreateReportCommand.Check()
            .WithOwnerName("CreateReport")
            .Field(EntityType).NotWhiteSpace()
            .Field(EntityId).NotEmptyGuid()
            .Field(ReasonCode).NotWhiteSpace();
}

internal sealed class CreateReportCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ISender sender,
    ILogger<CreateReportCommandHandler> logger)
    : ICommandHandler<CreateReportCommand, ReportDto>
{
    public async Task<Result<ReportDto, Error>> Handle(
        CreateReportCommand request,
        CancellationToken cancellationToken)
    {
        // Guard: self-ship orders cannot open generic disputes after the
        // buyer has accepted (order Completed). Return path stays available
        // via RequestOrderReturn. Non-self-ship and pre-accept orders are
        // unaffected.
        var entityTypeNormalized = request.EntityType.Trim().ToLowerInvariant();
        if (entityTypeNormalized == "order")
        {
            var orderId = OrderId.From(request.EntityId);
            var order = await dbContext.Set<Order>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

            if (order is not null && order.Status == OrderStatus.Completed)
            {
                var hasDirectShipment = await dbContext.Set<SellerDirectShipment>()
                    .AsNoTracking()
                    .AnyAsync(s => s.OrderId == orderId, cancellationToken);

                if (hasDirectShipment)
                {
                    return Error.Conflict(
                        "Report.SelfShipPostAcceptDisputeBlocked",
                        "Self-ship orders cannot open generic disputes after acceptance.");
                }
            }
        }

        var report = Report.Create(
            reporterId: currentUser.UserId,
            entityType: request.EntityType.Trim(),
            entityId: request.EntityId,
            reasonCode: request.ReasonCode.Trim(),
            description: request.Description,
            attachments: request.Attachments,
            nowUtc: clock.UtcNow);

        dbContext.Insert(report);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: currentUser.UserId.Value,
                NotificationType: "moderation",
                EventType: "report_created",
                Title: "Bao cao da duoc ghi nhan",
                Message: "He thong da ghi nhan bao cao cua ban va se xu ly som.",
                Priority: NotificationPriority.Normal,
                EntityType: "Report",
                EntityId: report.Id.Value),
            cancellationToken);

        return report.ToDto();
    }
}

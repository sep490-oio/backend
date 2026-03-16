using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.CreateAuctionAlert;

public sealed record CreateAuctionAlertCommand(
    Guid AuctionId,
    string AlertType,
    string Severity = "medium",
    string Payload = "{}") : ICommand<MonitoringAlertDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CreateAuctionAlertCommand.Check()
            .WithOwnerName("CreateAuctionAlert")
            .Field(AuctionId).NotEmptyGuid()
            .Field(AlertType).NotWhiteSpace()
            .Field(Severity).NotWhiteSpace();
}

internal sealed class CreateAuctionAlertCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ModerationAuditService auditService)
    : ICommandHandler<CreateAuctionAlertCommand, MonitoringAlertDto>
{
    public async Task<Result<MonitoringAlertDto, Error>> Handle(
        CreateAuctionAlertCommand request,
        CancellationToken cancellationToken)
    {
        var auctionExists = await dbContext.Set<Auction>()
            .AnyAsync(x => x.Id == AuctionId.From(request.AuctionId), cancellationToken);

        if (!auctionExists)
            return Error.NotFound("Auction.NotFound", "Auction was not found.");

        var severityResult = ModerationValueParsers.ParseAlertSeverity(request.Severity);
        if (severityResult.IsFailure)
            return severityResult.Error;

        var alert = MonitoringAlert.Create(
            entityType: "Auction",
            entityId: request.AuctionId,
            alertType: request.AlertType.Trim(),
            severity: severityResult.Value,
            payload: string.IsNullOrWhiteSpace(request.Payload) ? "{}" : request.Payload,
            nowUtc: clock.UtcNow);

        dbContext.Insert(alert);
        auditService.Log(
            action: "auction_alert_created",
            entityType: "Auction",
            entityId: request.AuctionId,
            newData: new
            {
                alertType = alert.AlertType,
                severity = alert.Severity.Id,
                payload = alert.Payload
            });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return alert.ToDto();
    }
}

using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.ResolveAuctionEmergency;

public sealed record ResolveAuctionEmergencyCommand(
    Guid AuctionId,
    Guid EmergencyId,
    string Status,
    object Payload) : ICommand<AuctionEmergencyDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return ResolveAuctionEmergencyCommand.Check()
            .WithOwnerName("ResolveAuctionEmergency")
            .Field(AuctionId).NotEmptyGuid()
            .Field(EmergencyId).NotEmptyGuid()
            .Field(Status).NotWhiteSpace()
            .InSet(EmergencyStatus.All.Select(x => x.Id));
    }
}

internal sealed class ResolveAuctionEmergencyCommandHandler
    : ICommandHandler<ResolveAuctionEmergencyCommand, AuctionEmergencyDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ModerationAuditService _auditService;

    public ResolveAuctionEmergencyCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ModerationAuditService auditService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _auditService = auditService;
    }

    public async Task<Result<AuctionEmergencyDto, Error>> Handle(
        ResolveAuctionEmergencyCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query.Include(a => a.Emergencies),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var status = EmergencyStatus.FromId(request.Status);
        if (status.HasNoValue)
            return Error.Validation("Status", "AuctionEmergency.InvalidStatus", "Unsupported emergency status.");

        var result = auction.ResolveEmergency(
            AuctionEmergencyId.From(request.EmergencyId),
            status.Value,
            JsonSerializer.Serialize(request.Payload),
            _clock.UtcNow);

        if (result.IsFailure)
            return result.Error;

        _auditService.Log(
            action: "auction_emergency_resolved",
            entityType: "AuctionEmergency",
            entityId: request.EmergencyId,
            newData: new
            {
                auctionId = request.AuctionId,
                emergencyId = request.EmergencyId,
                status = request.Status
            });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.ToDto();
    }
}

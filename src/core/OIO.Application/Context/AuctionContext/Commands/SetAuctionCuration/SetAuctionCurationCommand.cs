using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.SetAuctionCuration;

public sealed record SetAuctionCurationCommand(
    Guid AuctionId,
    Guid? AssignedAdminId = null,
    bool ClearAssignedAdmin = false,
    decimal? Priority = null,
    string? PriorityReason = null,
    bool? IsFeatured = null) : ICommand<AuctionDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return SetAuctionCurationCommand.Check()
            .WithOwnerName("SetAuctionCuration")
            .Field(AuctionId).NotEmptyGuid()
            .Field(AssignedAdminId).WhenHasValue(x => x.NotEmptyGuid())
            .Field(Priority).WhenHasValue(x => x.NonNegative())
            .Field(PriorityReason).WhenHasValue(x => x.NotWhiteSpace());
    }
}

internal sealed class SetAuctionCurationCommandHandler
    : ICommandHandler<SetAuctionCurationCommand, AuctionDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppConfigs _appConfigs;
    private readonly IClock _clock;

    public SetAuctionCurationCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IAppConfigs appConfigs,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _appConfigs = appConfigs;
        _clock = clock;
    }

    public async Task<Result<AuctionDto, Error>> Handle(
        SetAuctionCurationCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query.Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        UserId? assignedAdminId = request.ClearAssignedAdmin
            ? null
            : request.AssignedAdminId.HasValue
                ? UserId.From(request.AssignedAdminId.Value)
                : auction.AssignedAdminId;

        PriorityInfo? priority = null;
        if (request.Priority.HasValue || request.PriorityReason is not null)
        {
            priority = PriorityInfo.Create(
                request.Priority ?? auction.Priority?.Score ?? 0,
                request.PriorityReason ?? auction.Priority?.Reason ?? "{}");
        }

        var result = auction.ApplyCuration(
            assignedAdminId,
            priority,
            request.IsFeatured,
            nowUtc);

        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return auction.ToDto(
            nowUtc,
            await _appConfigs.Auctions.GetExtensionThresholdMinutesAsync(cancellationToken));
    }
}

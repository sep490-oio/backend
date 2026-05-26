using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminOverrideAuctionStatus;

public sealed record AdminOverrideAuctionStatusCommand(
    Guid AuctionId,
    string NewStatus,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        AdminOverrideAuctionStatusCommand.Check()
            .WithOwnerName("AdminOverrideAuctionStatus")
            .Field(AuctionId).NotEmptyGuid()
            .Field(NewStatus).NotWhiteSpace()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class AdminOverrideAuctionStatusCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<AdminOverrideAuctionStatusCommand>
{
    private static readonly Dictionary<string, AuctionStatus> StatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["draft"] = AuctionStatus.Draft,

        ["approved"] = AuctionStatus.Approved,
        ["scheduled"] = AuctionStatus.Scheduled,
        ["active"] = AuctionStatus.Active,
        ["ended"] = AuctionStatus.Ended,
        ["sold"] = AuctionStatus.Sold,
        ["completed"] = AuctionStatus.Completed,
        ["payment_defaulted"] = AuctionStatus.PaymentDefaulted,
        ["cancelled"] = AuctionStatus.Cancelled,
        ["failed"] = AuctionStatus.Failed,
        ["terminated"] = AuctionStatus.Terminated,
    };

    public async Task<UnitResult<Error>> Handle(
        AdminOverrideAuctionStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (!StatusMap.TryGetValue(request.NewStatus, out var newStatus))
            return Error.Validation("NewStatus", "Auction.InvalidStatus",
                $"'{request.NewStatus}' is not a valid auction status.");

        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query.Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var result = auction.AdminOverrideStatus(newStatus, $"[ADMIN] {request.Reason}", clock.UtcNow);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}

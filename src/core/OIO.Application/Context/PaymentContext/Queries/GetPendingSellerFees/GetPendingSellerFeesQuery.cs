using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetPendingSellerFees;

public sealed record GetPendingSellerFeesQuery : IQuery<IReadOnlyList<PendingSellerFeeDto>>;

internal sealed class GetPendingSellerFeesQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetPendingSellerFeesQuery, IReadOnlyList<PendingSellerFeeDto>>
{
    public async Task<Result<IReadOnlyList<PendingSellerFeeDto>, Error>> Handle(
        GetPendingSellerFeesQuery request, CancellationToken ct)
    {
        var pendingFees = await dbContext.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == currentUser.UserId
                        && t.Type == TransactionType.Fee
                        && t.Status == TransactionStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new PendingSellerFeeDto(
                t.Id.Value,
                t.TransactionNumber.Value,
                t.Amount.Amount,
                t.Currency,
                t.Description,
                t.CreatedAt,
                t.OrderId == null ? (Guid?)null : t.OrderId.Value.Value,
                t.AuctionId == null ? (Guid?)null : t.AuctionId.Value.Value
            ))
            .ToListAsync(ct);

        return pendingFees;
    }
}
